using Common.Summer.Core;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
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
        private AoiZone? m_aoiZoneManager = new AoiZone(0.001f, 0.001f); // AOI管理器：十字链表空间(unity坐标系)
        private Vector2 m_viewArea;
        private List<RevivalPointDefine> revivalPointDefines = new List<RevivalPointDefine>();

        public ConcurrentQueue<Action> actionQueue = new ConcurrentQueue<Action>();          // 任务队列,将space中的操作全部线性化，避免了并发问题

        #region Get

        public SceneCharacterManager SceneCharacterManager => m_sceneCharacterManager;
        public SceneMonsterManager SceneMonsterManager => m_scenenMonsterManager;
        public FightManager FightManager => m_fightManager;
        public int SceneId => m_sceneDefine.SID;
        public AoiZone AoiZone => m_aoiZoneManager;

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
            actionQueue.Clear();
            revivalPointDefines.Clear();
            Scheduler.Instance.RemoveTask(Update);
        }
        private void Update()
        {
            m_spawnManager.Update(MyTime.deltaTime);
            m_fightManager.Update(MyTime.deltaTime);
            while (actionQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public void CharacterEnterScene(Connection conn, CharacterEnterSceneRequest message)
        {
            Log.Information("a character enter scene");

            // 1.创建chr实例
            var gateConn = ServersMgr.Instance.GetGameGateConnByServerId(message.GameGateServerId);
            var chr = m_sceneCharacterManager.CreateSceneCharacter(message.SessionId, gateConn, message.DbChrNode);
            chr.CurSceneId = SceneId;

            // 2.加入aoi空间
            m_aoiZoneManager.Enter(chr);
            var handle = m_aoiZoneManager.Refresh(chr.EntityId, m_viewArea);
            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(handle.ViewEntity);

            // 3.新上线的玩家需要获取场景中:全部的角色/怪物/物品的信息
            SelfCharacterEnterSceneResponse sResp = new();
            sResp.TaskId = message.TaskId;
            sResp.ResultCode = 0;
            sResp.SelfNetActorNode = chr.NetActorNode;

            OtherEntityEnterSceneResponse oResp = new();
            oResp.SceneId = SceneId;
            oResp.EntityType = SceneEntityType.Actor;
            oResp.ActorNode = chr.NetActorNode;

            foreach (var ent in units)
            {
                if (ent is SceneActor acotr)
                {
                    sResp.OtherNetActorNodeList.Add(acotr.NetActorNode);
                }else if(ent is SceneItem item)
                {
                    sResp.OtherNetItemNodeList.Add(item.NetItemNode);
                }

                // 4.通知附近玩家
                if(ent is SceneCharacter sChr)
                {
                    // 刷新他的aoi，以免下次使用时错误判断self是新加入的
                    m_aoiZoneManager.Refresh(sChr.EntityId, m_viewArea); 

                    oResp.SessionId = sChr.SessionId;
                    sChr.Send(oResp);
                }
            }
            conn.Send(sResp);
        }
        public void CharacterLeaveScene(int entityId)
        {
            var chr = m_sceneCharacterManager.GetSceneCharacterByEntityId(entityId);
            if(chr == null)
            {
                goto End;
            }
            Log.Information("a character leave scene");

            // 广播通知其他玩家
            var resp = new OtherEntityLeaveSceneResponse();
            resp.SceneId = SceneId;
            resp.EntityId = chr.EntityId;
            var views = m_aoiZoneManager.FindViewEntity(chr.EntityId, false);
            foreach (var cc in views.OfType<SceneCharacter>())
            {
                resp.SessionId = cc.SessionId;
                cc.Send(resp);
            }

            // 回收
            m_sceneCharacterManager.RemoveSceneCharacterByEntityId(entityId);

            // 退出aoi空间
            m_aoiZoneManager.Exit(chr.EntityId);
        End:
            return;
        }
        public void MonsterEnterScene(SceneMonster monster)
        {

        }
        public void MonsterExitScene()
        {

        }
        public void ItemEnterScene(SceneItem sceneItem)
        {

        }
        public void ItemExitScene(int entityId)
        {

        }

        public void ActorChangeMode(SceneActor self, ActorChangeModeRequest message, bool isIncludeSelf = false)
        {
            // 保存与角色的相关信息
            self.ChangeActorMode(message.Mode);
            Log.Information("actor[entityId = {0}] change mode {1}", self.EntityId, message.Mode);

            // 通知附近玩家
            var resp = new ActorChangeModeResponse();
            resp.EntityId = self.EntityId;
            resp.Mode = message.Mode;
            resp.Timestamp = message.Timestamp;

            var all = m_aoiZoneManager.FindViewEntity(self.EntityId, isIncludeSelf);
            foreach (var chr in all.OfType<SceneCharacter>())
            {
                resp.SessionId = chr.SessionId;
                chr.Send(resp);
            }
        }
        public void ActorChangeState(SceneActor self, ActorChangeStateRequest message, bool isIncludeSelf = false)
        {
            // 保存与角色的相关信息
            self.SetTransform(message.OriginalTransform);
            self.ChangeActorState(message.State);
            Log.Information("actor[entityId = {0}] change state {1}", self.EntityId, message.State);

            // 更新aoi空间里面我们的坐标
            var handle = m_aoiZoneManager?.UpdatePos_Refresh(self.EntityId, self.AoiPos.x, self.AoiPos.y, m_viewArea);
            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(handle.ViewEntity);

            // 通知附近玩家
            var resp = new ActorChangeStateResponse();
            resp.EntityId = self.EntityId;
            resp.State = message.State;
            resp.OriginalTransform = message.OriginalTransform;
            resp.Timestamp = message.Timestamp;
            resp.PayLoad = message.PayLoad;

            // 告知view内的其他角色，状态变更
            foreach (var sChr in units.OfType<SceneCharacter>())
            {
                resp.SessionId = sChr.SessionId;
                sChr.Send(resp);
            }
            if (isIncludeSelf && self is SceneCharacter ssChr)
            {
                resp.SessionId = ssChr.SessionId;
                ssChr.Send(resp);
            }

            // 通知各palyer视野变更
            var enterResp = new OtherEntityEnterSceneResponse();
            enterResp.SceneId = SceneId;

            var leaveResp = new OtherEntityLeaveSceneResponse();
            leaveResp.SceneId = SceneId;
            
            if (self is SceneCharacter selfChr)
            {
                //新进入视野的单位，双向通知
                enterResp.EntityType = SceneEntityType.Actor;
                enterResp.ActorNode = selfChr.NetActorNode;
                foreach (var key in handle.Newly)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);
                    if (entity is SceneCharacter targetChr)
                    {
                        //告诉对方自己已经进入对方视野
                        enterResp.SessionId = targetChr.SessionId;
                        targetChr.Send(enterResp);

                        //需要告诉自己,目标加入了我们的视野
                        enterResp.ActorNode = targetChr.NetActorNode;
                        enterResp.SessionId = selfChr.SessionId;
                        selfChr.Send(enterResp);
                    }
                    else if (entity is SceneMonster targetMon)
                    {
                        //需要告诉自己,目标加入了我们的视野
                        enterResp.ActorNode = selfChr.NetActorNode;
                        enterResp.ActorNode = targetMon.NetActorNode;
                        enterResp.SessionId = selfChr.SessionId;
                        selfChr.Send(enterResp);
                    }
                    else if (entity is SceneNpc targetNpc)
                    {
                        //需要告诉自己,目标加入了我们的视野
                        enterResp.ActorNode = selfChr.NetActorNode;
                        enterResp.ActorNode = targetNpc.NetActorNode;
                        enterResp.SessionId = selfChr.SessionId;
                        selfChr.Send(enterResp);
                    }
                    else if (entity is SceneItem ie)
                    {

                    }
                }

                // 远离视野的单位，双向通知
                foreach (var key in handle.Leave)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);
                    if (entity is SceneCharacter targetChr)
                    {
                        //告诉他,自己已经离开他的视野
                        leaveResp.EntityId = selfChr.EntityId;
                        leaveResp.SessionId = targetChr.SessionId;
                        targetChr.Send(leaveResp);
                    }
                    // 同时告诉自己对方离开自己视野
                    leaveResp.EntityId = (int)key;
                    leaveResp.SessionId = selfChr.SessionId;
                    selfChr.Send(leaveResp);
                }
            }
            else if (self is SceneMonster || self is SceneNpc)
            {
                //新进入视野的单位，双向通知
                foreach (var key in handle.Newly)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);

                    //如果对方是Character
                    if (entity is SceneCharacter targetChr)
                    {
                        // 告诉targetChr,自己已经进入他的视野
                        enterResp.EntityType = SceneEntityType.Actor;
                        enterResp.ActorNode = self.NetActorNode;
                        enterResp.SessionId = targetChr.SessionId;
                        targetChr.Send(enterResp);
                    }
                }

                //远离视野的单位，双向通知
                foreach (var key in handle.Leave)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);

                    //如果对方是玩家
                    if (entity is SceneCharacter targetChr)
                    {
                        //告诉他,自己已经离开他的视野
                        leaveResp.EntityId = self.EntityId;
                        leaveResp.SessionId = targetChr.SessionId;
                        targetChr.Send(leaveResp);
                    }

                }
            }

        }
        public void ActorChangeTransformData(SceneActor self, ActorChangeTransformDataRequest message, bool isIncludeSelf = false)
        {
            // 改变相关信息
            self.SetTransform(message.OriginalTransform);
            if(self.NetActorState == NetActorState.Motion)
            {
                self.Speed = message.PayLoad.VerticalSpeed;
            }
            // Log.Information("actor[entityId = {0}] change transform data", self.EntityId);

            // 更新aoi空间里面我们的坐标
            var handle = m_aoiZoneManager?.UpdatePos_Refresh(self.EntityId, self.AoiPos.x, self.AoiPos.y, m_viewArea);
            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(handle.ViewEntity);

            // 通知附近玩家Transform数据更新
            var resp = new ActorChangeTransformDataResponse();
            resp.EntityId = self.EntityId;
            resp.OriginalTransform = message.OriginalTransform;
            resp.Timestamp = message.Timestamp;
            resp.PayLoad = message.PayLoad;
            foreach (var chr in units.OfType<SceneCharacter>())
            {
                resp.SessionId = chr.SessionId;
                chr.Send(resp);
            }
            if (isIncludeSelf && self is SceneCharacter ssChr)
            {
                resp.SessionId = ssChr.SessionId;
                ssChr.Send(resp);
            }

            // 通知各palyer视野变更
            var enterResp = new OtherEntityEnterSceneResponse();
            enterResp.SceneId = SceneId;

            var leaveResp = new OtherEntityLeaveSceneResponse();
            leaveResp.SceneId = SceneId;

            if (self is SceneCharacter selfChr)
            {
                //新进入视野的单位，双向通知
                enterResp.EntityType = SceneEntityType.Actor;
                enterResp.ActorNode = selfChr.NetActorNode;
                foreach (var key in handle.Newly)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);
                    if (entity is SceneCharacter targetChr)
                    {
                        //告诉对方自己已经进入对方视野
                        enterResp.SessionId = targetChr.SessionId;
                        targetChr.Send(enterResp);

                        //需要告诉自己,目标加入了我们的视野
                        enterResp.ActorNode = targetChr.NetActorNode;
                        enterResp.SessionId = selfChr.SessionId;
                        selfChr.Send(enterResp);
                    }
                    else if (entity is SceneMonster targetMon)
                    {
                        //需要告诉自己,目标加入了我们的视野
                        enterResp.ActorNode = selfChr.NetActorNode;
                        enterResp.ActorNode = targetMon.NetActorNode;
                        enterResp.SessionId = selfChr.SessionId;
                        selfChr.Send(enterResp);
                    }
                    else if (entity is SceneNpc targetNpc)
                    {
                        //需要告诉自己,目标加入了我们的视野
                        enterResp.ActorNode = selfChr.NetActorNode;
                        enterResp.ActorNode = targetNpc.NetActorNode;
                        enterResp.SessionId = selfChr.SessionId;
                        selfChr.Send(enterResp);
                    }
                    else if (entity is SceneItem ie)
                    {

                    }
                }

                // 远离视野的单位，双向通知
                foreach (var key in handle.Leave)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);
                    if (entity is SceneCharacter targetChr)
                    {
                        //告诉他,自己已经离开他的视野
                        leaveResp.EntityId = selfChr.EntityId;
                        leaveResp.SessionId = targetChr.SessionId;
                        targetChr.Send(leaveResp);
                    }
                    // 同时告诉自己对方离开自己视野
                    leaveResp.EntityId = (int)key;
                    leaveResp.SessionId = selfChr.SessionId;
                    selfChr.Send(leaveResp);
                }
            }
            else if (self is SceneMonster || self is SceneNpc)
            {
                //新进入视野的单位，双向通知
                foreach (var key in handle.Newly)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);

                    //如果对方是Character
                    if (entity is SceneCharacter targetChr)
                    {
                        // 告诉targetChr,自己已经进入他的视野
                        enterResp.EntityType = SceneEntityType.Actor;
                        enterResp.ActorNode = self.NetActorNode;
                        enterResp.SessionId = targetChr.SessionId;
                        targetChr.Send(enterResp);
                    }
                }

                //远离视野的单位，双向通知
                foreach (var key in handle.Leave)
                {
                    var entity = SceneEntityManager.Instance.GetSceneEntityById((int)key);

                    //如果对方是玩家
                    if (entity is SceneCharacter targetChr)
                    {
                        //告诉他,自己已经离开他的视野
                        leaveResp.EntityId = self.EntityId;
                        leaveResp.SessionId = targetChr.SessionId;
                        targetChr.Send(leaveResp);
                    }

                }
            }
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
        public void TransmitTo(SceneCharacter chr, int PointId)
        {

        }
    }
}
