
using Common.Summer.Core;
using GameServer.Core.Task;
using GameServer.Core.Task.Event;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;

namespace GameServer.Core.Model
{
    public class GameCharacter
    {
        private int m_entityId;
        private int m_curSceneId;
        private DBCharacterNode m_dbChr;
        private Connection relativeGateConnection;
        private string sessionId;
        private CharacterEventSystem m_characterEventSystem;
        private GameTaskManager m_gameTaskManager;

        // equips
        // 背包
        // chat

        #region
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
        #endregion

        public GameCharacter(DBCharacterNode dbChr)
        {
            this.m_dbChr = dbChr;

            m_curSceneId = dbChr.ChrStatus.CurSceneId;

            m_characterEventSystem = new();

            m_gameTaskManager = new GameTaskManager();
            m_gameTaskManager.Init(this, dbChr.ChrTasks);
        }
        public void SaveGameCharacter()
        {
            // 保存一部分信息

            // 任务
        }
        public void Send(IMessage message)
        {
            relativeGateConnection.Send(message);
        }
    }
}
