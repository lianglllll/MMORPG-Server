using Common.Summer.Core;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.AOI;
using SceneServer.Core.Model;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Model.Item;
using SceneServer.Net;
using SceneServer.Utils;
using Serilog;
using System.Diagnostics;

namespace SceneServer.Core.Scene
{
    public class SceneManager : Singleton<SceneManager>
    {
        private SpaceDefine? m_sceneDefine;
        private SceneCharacterManager? m_sceneCharacterManager;
        private AoiZone? m_aoiZone;                                             //AOI管理器：十字链表空间(unity坐标系)
        private Vector2? viewArea;

        public void Init(int sceneId)
        {
            m_sceneDefine = StaticDataManager.Instance.sceneDefineDict[sceneId];
            m_sceneCharacterManager = new();
            m_sceneCharacterManager.Init();
            m_aoiZone = new AoiZone(0.001f, 0.001f);
            viewArea = new(Config.Server.aoiViewArea, Config.Server.aoiViewArea);

            // 添加自循环
            Scheduler.Instance.AddTask(Update, Config.Server.updateHz, 0);
        }
        public void UnInit()
        {
            // 添加自循环
            Scheduler.Instance.RemoveTask(Update);
        }
        private void Update()
        {
        }

        public int SceneId => m_sceneDefine.SID;
        public AoiZone AoiZone => m_aoiZone;

        public void CharacterEnterScene(Connection conn, CharacterEnterSceneRequest message)
        {
            Log.Information("a character enter scene");

            // 1.创建chr实例
            var gateConn = ServersMgr.Instance.GetGameGateConnByServerId(message.GameGateServerId);
            var chr = m_sceneCharacterManager.CreateSceneCharacter(message.SessionId, gateConn, message.DbChrNode);
            chr.CurSceneId = SceneId;

            // 2.加入aoi空间
            m_aoiZone.Enter(chr);

            // 3.新上线的玩家需要获取场景中:全部的角色/怪物/物品的信息
            SelfCharacterEnterSceneResponse sResp = new();
            sResp.TaskId = message.TaskId;
            sResp.ResultCode = 0;
            sResp.SelfNetActorNode = chr.NetActorNode;
            var nearbyEntity = m_aoiZone.FindViewEntity(chr.EntityId);
            foreach (var ent in nearbyEntity)
            {
                if (ent is SceneActor acotr)
                {
                    sResp.OtherNetActorNodeList.Add(acotr.NetActorNode);
                }else if(ent is SceneItem item)
                {
                    sResp.OtherNetItemNodeList.Add(item.NetItemNode);
                }
            }
            conn.Send(sResp);

            // 4.通知附近玩家
            OtherEntityEnterSceneResponse oResp = new();
            oResp.SceneId = SceneId;
            oResp.EntityType = SceneEntityType.Actor;
            oResp.ActorNode = chr.NetActorNode;
            var views = nearbyEntity.ToList();
            foreach (var oChr in views.OfType<SceneCharacter>())
            {
                oResp.SessionId = oChr.SessionId;
                oChr.Send(oResp);
            }
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
            var views = m_aoiZone.FindViewEntity(chr.EntityId, false);
            foreach (var cc in views.OfType<SceneCharacter>())
            {
                resp.SessionId = cc.SessionId;
                cc.Send(resp);
            }

            // 回收
            m_sceneCharacterManager.RemoveSceneCharacterByEntityId(entityId);

            // 退出aoi空间
            m_aoiZone.Exit(chr.EntityId);
        End:
            return;
        }
        public void MonsterEnterScene()
        {

        }
        public void MonsterExitScene()
        {

        }
        public void ItemEnterScene()
        {

        }
        public void ItemExitScene()
        {

        }

        public void ActorChangeState(SceneActor self, ActorChangeStateRequest message, bool isIncludeSelf = false)
        {
            // 保存与角色的相关信息
            self.SetTransform(message.OriginalTransform);
            self.ChangeActorState(message.State);
            Log.Information("actor[entityId = {0}] change state {1}", self.EntityId, message.State);

            // 通知附近玩家
            var resp = new ActorChangeStateResponse();
            resp.EntityId = self.EntityId;
            resp.State = message.State;
            resp.OriginalTransform = message.OriginalTransform;
            resp.Timestamp = message.Timestamp;

            var all = m_aoiZone.FindViewEntity(self.EntityId, isIncludeSelf);
            foreach (var chr in all.OfType<SceneCharacter>())
            {
                resp.SessionId = chr.SessionId;
                chr.Send(resp);
            }
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

            var all = m_aoiZone.FindViewEntity(self.EntityId, isIncludeSelf);
            foreach (var chr in all.OfType<SceneCharacter>())
            {
                resp.SessionId = chr.SessionId;
                chr.Send(resp);
            }
        }
        internal void ActorChangeMotionData(SceneActor self, ActorChangeMotionDataRequest message, bool isIncludeSelf = false)
        {
            // 改变相关信息
            self.SetTransform(message.OriginalTransform);
            self.Speed = message.Speed;
            Log.Information("actor[entityId = {0}] change motion data", self.EntityId);

            // 通知附近玩家
            var resp = new ActorChangeMotionDataResponse();
            resp.EntityId = self.EntityId;
            resp.OriginalTransform = message.OriginalTransform;
            resp.Timestamp = message.Timestamp;
            resp.Speed = message.Speed;

            var all = m_aoiZone.FindViewEntity(self.EntityId, isIncludeSelf);
            foreach (var chr in all.OfType<SceneCharacter>())
            {
                resp.SessionId = chr.SessionId;
                chr.Send(resp);
            }

        }
    }
}
