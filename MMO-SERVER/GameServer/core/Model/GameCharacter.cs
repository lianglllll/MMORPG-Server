
using Common.Summer.Core;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;

namespace GameServer.Core.Model
{
    public class GameCharacter
    {
        private int m_entityId;
        private int m_curSceneId;
        private DBCharacterNode dbChr;
        private Connection relativeGateConnection;
        private string sessionId;

        // level exp
        // equips  背包
        // chat

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
        public string Cid => dbChr.CId;
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
        public string ChrName => dbChr.ChrName;

        public GameCharacter(DBCharacterNode dbChr)
        {
            this.dbChr = dbChr;
            m_curSceneId = dbChr.ChrStatus.CurSceneId;
        }

        public void Send(IMessage message)
        {
            relativeGateConnection.Send(message);
        }
    }
}
