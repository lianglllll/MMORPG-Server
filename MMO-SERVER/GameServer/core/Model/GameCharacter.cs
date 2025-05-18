using Common.Summer.Core;
using GameServer.Core.Task;
using GameServer.Core.Task.Event;
using GameServer.InventorySystem;
using GameServer.Net;
using Google.Protobuf;
using HS.Protobuf.Backpack;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.SceneEntity;
using System.Collections.Generic;
using System.IO;

namespace GameServer.Core.Model
{
    public class GameCharacter
    {
        private int m_entityId;
        private int m_curSceneId;
        private string sessionId;
        private DBCharacterNode m_dbChr;
        private Connection relativeGateConnection;
        private CharacterEventSystem    m_characterEventSystem;     // todo 有些事件需要跨服务器的，比如说game服里面的任务需要scene服里面的角色位置。
        private GameTaskManager         m_gameTaskManager;
        private EquipmentManager        m_equipmentManager;
        private InventoryManager        m_backPackManager;

        // chat信息

        #region GetSet
        public int EntityId
        {
            get { return m_entityId; }
            set { m_entityId = value; }
        }   
        public int CurSceneId
        {
            get { return m_curSceneId; }
            set { m_curSceneId = value; }
        }
        public string Cid => m_dbChr.CId;
        public Connection RelativeGateConnection
        {
            get => relativeGateConnection;
            set => relativeGateConnection = value;
        }
        public string SessionId
        {
            get => sessionId;
            set => sessionId = value;   
        }
        public string ChrName => m_dbChr.ChrName;
        public int Level
        {
            get => m_dbChr.Level;
            set => m_dbChr.Level = value;
        }
        public int Exp
        {
            get => m_dbChr.ChrStatus.Exp;
            set => m_dbChr.ChrStatus.Exp = value;
        }
        public CharacterEventSystem CharacterEventSystem => m_characterEventSystem;
        public GameTaskManager GameTaskManager => m_gameTaskManager;
        public DBCharacterNode DBCharacterNode => m_dbChr;
        public InventoryManager BackPackManager => m_backPackManager;
        public EquipmentManager EquipmentManager => m_equipmentManager;
        #endregion
        #region 生命周期
        public GameCharacter(DBCharacterNode dbChr)
        {
            this.m_dbChr = dbChr;
            m_curSceneId = dbChr.ChrStatus.CurSceneId;
            m_characterEventSystem  = new();
            m_gameTaskManager = new();
            m_gameTaskManager.Init(this, dbChr.ChrTasks);
            m_equipmentManager = new();
            m_equipmentManager.Init(this, dbChr.ChrInventorys.EquipsData.ToByteArray());
            m_backPackManager = new();
            m_backPackManager.Init(this, ItemInventoryType.Backpack, dbChr.ChrInventorys.BackpackData.ToByteArray());
        }
        #endregion
        #region tools
        public void SaveGameCharacter(HS.Protobuf.Scene.CharacterLeaveSceneResponse message)
        {
            var req = new SaveDBCharacterRequest();
            req.CNode = DBCharacterNode;

            // 场景相关信息
            var chrStatus = req.CNode.ChrStatus;
            chrStatus.CurSceneId = CurSceneId;
            chrStatus.X = message.SceneSaveDatea.Position.X;
            chrStatus.Y = message.SceneSaveDatea.Position.Y;
            chrStatus.Z = message.SceneSaveDatea.Position.Z;
            
            // 任务
            req.CNode.ChrTasks = GameTaskManager.GetDBTaskNodes();

            // 背包
            var chrInventorys = new DBInventorys();
            req.CNode.ChrInventorys = chrInventorys;
            if (m_backPackManager.HasChanged)
            {
                using (var ms = new MemoryStream())
                {
                    m_backPackManager.NetItemInventoryDataNode.WriteTo(ms);
                    chrInventorys.BackpackData = ByteString.FromStream(ms);
                }
            }
            if (m_equipmentManager.HasChanged)
            {
                using (var ms = new MemoryStream())
                {
                    m_equipmentManager.NetItemInventoryDataNode.WriteTo(ms);
                    chrInventorys.EquipsData = ByteString.FromStream(ms);
                }
            }

            ServersMgr.Instance.SendMsgToDBProxy(req);

        }
        public void SendToGate(IMessage message)
        {
            relativeGateConnection.Send(message);
        }
        public void SendToScene(IMessage message)
        {
            var sceneConn = GameMonitor.Instance.GetSceneConnBySceneId(m_curSceneId);
            sceneConn.Send(message);
        }
        #endregion
    }
}
