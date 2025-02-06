using Common.Summer.Core;
using Common.Summer.Tools;
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
            m_aoiZone.Enter(chr);
            chr.CurSceneId = SceneId;

            //2.新上线的玩家需要获取场景中:全部的角色/怪物/物品的信息
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

            //3.通知附近玩家
            OtherEntityEnterSceneResponse oResp = new();
            oResp.SceneId = SceneId;
            oResp.EntityType = SceneEntityType.Actor;
            oResp.ActorNode = chr.NetActorNode;
            var views = nearbyEntity.ToList();
            foreach (var oChr in views.OfType<SceneCharacter>())
            {
                oChr.Send(oResp);
            }
        }
        public void CharacterLeaveScene(int entityId)
        {
            Log.Information("a character leave scene");
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

    }
}
