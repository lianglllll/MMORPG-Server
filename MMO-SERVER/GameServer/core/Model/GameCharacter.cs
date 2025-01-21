
using HS.Protobuf.DBProxy.DBCharacter;

namespace GameServer.Core.Model
{
    public class GameCharacter
    {
        private DBCharacterNode dbChr;

        public GameCharacter(DBCharacterNode dbChr)
        {
            this.dbChr = dbChr;
        }
    }
}
