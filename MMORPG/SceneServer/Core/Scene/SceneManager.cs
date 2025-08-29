using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.AOIMap.NineSquareGrid;
using SceneServer.Core.AOI;
using SceneServer.Core.Combat;
using SceneServer.Core.Model;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Model.Item;
using SceneServer.Core.Scene.Component;
using SceneServer.Net;
using SceneServer.Utils;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using YamlDotNet.Core.Tokens;

namespace SceneServer.Core.Scene
{
    public class SceneManager : Singleton<SceneManager>
    {
        private SceneCharacterManager   m_sceneCharacterManager = new();
        private SceneMonsterManager     m_scenenMonsterManager  = new();        
        private SceneItemManager        m_sceneItemManager      = new();
        private SpawnManager            m_spawnManager          = new();
        private FightManager            m_fightManager          = new();         // 战斗管理器，负责技能、投射物、伤害、actor信息的更新

        private SpaceDefine? m_sceneDefine;
        private AOIManager<SceneEntity> m_aoiManager;
        private Vector2 m_viewArea;
        private List<RevivalPointDefine> revivalPointDefines = new List<RevivalPointDefine>();

        private ConcurrentQueue<Action> m_actionQueue = new ConcurrentQueue<Action>();          // 任务队列,将scene中的操作全部线性化，避免了并发问题(消息路由和中心调度的并发)

        #region GetSet
        public SceneCharacterManager SceneCharacterManager => m_sceneCharacterManager;
        public SceneMonsterManager SceneMonsterManager => m_scenenMonsterManager;
        public SceneItemManager SceneItemManager => m_sceneItemManager;
        public FightManager FightManager => m_fightManager;
        public int SceneId => m_sceneDefine.SID;
        public AOIManager<SceneEntity> AOIManager => m_aoiManager;
        public Vector2 ViewArea => m_viewArea;
        #endregion

        public void Init(int sceneId)
        {
            m_sceneDefine = StaticDataManager.Instance.sceneDefineDict[sceneId];
            m_viewArea = new(Config.Server.aoiViewArea, Config.Server.aoiViewArea);
            foreach (var pointId in m_sceneDefine.RevivalPointS)
            {
                var pointDef = StaticDataManager.Instance.revivalPointDefineDict[pointId];
                if (pointDef == null) continue;
                revivalPointDefines.Add(pointDef);
            }

            m_aoiManager = new AOIManager<SceneEntity>(m_sceneDefine.Area[0], m_sceneDefine.Area[1], m_sceneDefine.Area[2], m_sceneDefine.Area[3]);
            m_sceneCharacterManager.Init();
            m_scenenMonsterManager.Init();
            m_sceneItemManager.Init();
            m_spawnManager.Init();
            m_fightManager.Init();

            // 添加自循环
            Scheduler.Instance.Update(Update);
        }
        public void UnInit()
        {
            m_sceneCharacterManager.UnInit();
            m_scenenMonsterManager.UnInit();
            m_sceneItemManager.UnInit();
            m_spawnManager.UnInit();
            m_fightManager.UnInit();
            m_actionQueue.Clear();
            revivalPointDefines.Clear();
            Scheduler.Instance.RemoveTask(Update);
        }
        private void Update()
        {
            while (m_actionQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
            m_spawnManager.Update(MyTime.deltaTime);
            m_fightManager.Update(MyTime.deltaTime);
        }

        public void CharacterEnterScene(Connection gameConn, CharacterEnterSceneRequest message)
        {
            m_actionQueue.Enqueue(() => {
                Log.Information("a character enter scene");

                // 1.创建chr实例
                var gateConn = ServersMgr.Instance.GetGameGateConnByServerId(message.GameGateServerId);
                var chr = m_sceneCharacterManager.CreateSceneCharacter(message.SessionId, gateConn, message);
                chr.CurSceneId = SceneId;

                // 2.新上线的玩家需要获取场景中:全部的角色/怪物/物品的信息
                SelfCharacterEnterSceneResponse sResp = new();
                sResp.TaskId = message.TaskId;
                sResp.ResultCode = 0;
                sResp.SelfNetActorNode = chr.NetActorNode;
                var units = m_aoiManager.GetAOIUnits(chr.AoiPos);
                foreach (var ent in units)
                {
                    if (ent is SceneActor acotr)
                    {
                        sResp.OtherNetActorNodeList.Add(acotr.NetActorNode);
                    }
                    else if (ent is SceneItem item)
                    {
                        sResp.OtherNetItemNodeList.Add(item.NetItemNode);
                    }
                }
                gameConn.Send(sResp);

                // 3.加入aoi空间并会通知视野内的玩家
                m_aoiManager.Enter(chr);
            });
        }
        public void CharacterExitScene(int entityId)
        {
            m_actionQueue.Enqueue(() => {
                var chr = m_sceneCharacterManager.GetSceneCharacterByEntityId(entityId);
                if (chr == null)
                {
                    goto End;
                }
                Log.Information("a character leave scene");

                // 回收
                m_sceneCharacterManager.RemoveSceneCharacterByEntityId(entityId);

                // 退出aoi空间
                m_aoiManager.Exit(chr);
            End:
                return;
            });
        }
        public void MonsterEnterScene(SceneMonster monster)
        {
            m_actionQueue.Enqueue(() => {
                // 加入aoi空间并会通知视野内的玩家
                m_aoiManager.Enter(monster);
            });
        }
        public void MonsterExitScene(int entityId)
        {
            m_actionQueue.Enqueue(() => {

            });
        }
        public void ItemEnterScene(SceneItem sceneItem)
        {
            m_actionQueue.Enqueue(() => {
                AOIManager.Enter(sceneItem);
            });
        }
        public void ItemExitScene(SceneItem item)
        {
            m_actionQueue.Enqueue(() => {
                m_aoiManager.Exit(item);
            });
        }

        public void ActorChangeMode(SceneActor self, ActorChangeModeRequest message, bool isIncludeSelf = false)
        {
            m_actionQueue.Enqueue(() => {
                // 保存与角色的相关信息
                self.ChangeActorMode(message.Mode);
                // Log.Information("actor[entityId = {0}] change mode {1}", self.EntityId, message.Mode);

                // 通知附近玩家
                var resp = new ActorChangeModeResponse();
                resp.EntityId = self.EntityId;
                resp.Mode = message.Mode;
                resp.Timestamp = message.Timestamp;
                resp.SceneId = SceneId;

                var units = m_aoiManager.GetAOIUnits(self.AoiPos);
                foreach (var chr in units.OfType<SceneCharacter>())
                {
                    if(chr.EntityId == self.EntityId && !isIncludeSelf) {
                        continue;
                    }
                    resp.SessionId = chr.SessionId;
                    chr.Send(resp);
                }
            });
        }
        public void ActorChangeState(SceneActor self, ActorChangeStateRequest message, bool isIncludeSelf = false)
        {
            m_actionQueue.Enqueue(() => {
                // Log.Information("actor[entityId = {0}] change state {1}", self.EntityId, message.State);

                // 保存与角色的相关信息
                // 更新aoi空间里面我们的坐标
                var oldPos = self.AoiPos;
                self.SetTransform(message.OriginalTransform);
                var newPos = self.AoiPos;
                m_aoiManager.Move(self, oldPos, newPos);
                self.ChangeActorState(message.State);

                var units = m_aoiManager.GetAOIUnits(self.AoiPos);
                
                // 通知附近玩家
                var resp = new ActorChangeStateResponse();
                resp.EntityId = self.EntityId;
                resp.State = message.State;
                resp.OriginalTransform = message.OriginalTransform;
                resp.Timestamp = message.Timestamp;
                resp.PayLoad = message.PayLoad;
                resp.SceneId = SceneId;

                // 告知view内的其他角色，状态变更
                foreach (var chr in units.OfType<SceneCharacter>())
                {
                    if (chr.EntityId == self.EntityId && !isIncludeSelf)
                    {
                        continue;
                    }
                    resp.SessionId = chr.SessionId;
                    chr.Send(resp);
                }
            });
        }
        public void ActorChangeTransformData(SceneActor self, ActorChangeTransformDataRequest message, bool isIncludeSelf = false)
        {
            m_actionQueue.Enqueue(() => {

                // Log.Information("actor[entityId = {0}] change transform data", self.EntityId);

                // 改变相关信息
                if (self.NetActorState == NetActorState.Motion && self is SceneCharacter)
                {
                    self.Speed = message.PayLoad.VerticalSpeed;
                }

                // 更新aoi空间里面我们的坐标
                var oldPos = self.AoiPos;
                self.SetTransform(message.OriginalTransform);
                var newPos = self.AoiPos;
                m_aoiManager.Move(self, oldPos, newPos);

                // 通知附近玩家Transform数据更新
                var resp = new ActorChangeTransformDataResponse();
                resp.EntityId = self.EntityId;
                resp.OriginalTransform = message.OriginalTransform;
                resp.Timestamp = message.Timestamp;
                resp.PayLoad = message.PayLoad;
                resp.SceneId = SceneId;

                var units = m_aoiManager.GetAOIUnits(self.AoiPos);
                foreach (var chr in units.OfType<SceneCharacter>())
                {
                    if (chr.EntityId == self.EntityId && !isIncludeSelf)
                    {
                        continue;
                    }
                    resp.SessionId = chr.SessionId;
                    chr.Send(resp);
                }
            });
        }

        public void TransmitTo(SceneCharacter chr, int PointId)
        {
            m_actionQueue.Enqueue(() => {

            });
        }
        public void Broadcast(int entityId, bool isIncludeSelf, IMessage message)
        {
            m_actionQueue.Enqueue(() => {
                var self = SceneEntityManager.Instance.GetSceneEntityById(entityId);
                var units = m_aoiManager.GetAOIUnits(self.AoiPos);
                if (isIncludeSelf)
                {
                    units.Add(self);
                }

                var resp = new Scene2GateMsg();
                resp.Content = ByteString.CopyFrom(ProtoHelper.Instance.IMessageParse2ByteArray(message));
                foreach (var chr in units.OfType<SceneCharacter>())
                {
                    chr.Send(resp);
                }
            End:
                return;
            });
        }

        // tools
        public Vector3Int GetNearestRevivalPoint(SceneCharacter chr)
        {
            float comparativetGap = float.MaxValue;
            Vector3Int pos = chr.Position;
            foreach (var pointDef in revivalPointDefines)
            {
                var tempPos = new Vector3Int(pointDef.X, pointDef.Y, pointDef.Z);
                var gap = Vector3Int.Distance(chr.Position, tempPos);
                if (gap < comparativetGap)
                {
                    comparativetGap = gap;
                    pos = tempPos;
                }
            }
            return pos;
        }
        public List<SceneEntity> GetAoiEntitysById(int entityId)
        {
            List<SceneEntity> result = null;
            var entity = SceneEntityManager.Instance.GetSceneEntityById(entityId);
            if(entity == null) {
                result = new List<SceneEntity>();
                goto End;
            }
            result = m_aoiManager.GetAOIUnits(entity.AoiPos);
        End:
            return result;
        }
    }
}
