
using HS.Protobuf.DBProxy.DBCharacter;

namespace GameServer.Core.Model
{
    public class GameCharacter
    {
        private int m_entityId;
        private int m_curSceneId;
        private DBCharacterNode dbChr;

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


        public GameCharacter(DBCharacterNode dbChr)
        {
            this.dbChr = dbChr;
        }
    }
}
