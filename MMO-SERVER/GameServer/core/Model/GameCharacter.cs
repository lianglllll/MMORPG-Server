
using HS.Protobuf.DBProxy.DBCharacter;

namespace GameServer.Core.Model
{
    public class GameCharacter
    {
        private DBCharacterNode dbChr;

        // level exp
        // equips  背包
        // chat

        public GameCharacter(DBCharacterNode dbChr)
        {
            this.dbChr = dbChr;
        }
    }
}
