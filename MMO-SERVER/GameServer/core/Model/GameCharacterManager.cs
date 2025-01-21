using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using System.Collections.Concurrent;

namespace GameServer.Core.Model
{
    public class GameCharacterManager : Singleton<GameCharacterManager>
    {
        private ConcurrentDictionary<string, GameCharacter> characterDict = new();

        public void Init()
        {
            characterDict.Clear();
        }
        public void UnInit()
        {

        }

        public GameCharacter CreateGameCharacter(DBCharacterNode dbChr)
        {
            var gChr = new GameCharacter(dbChr);
            characterDict.TryAdd(dbChr.CId, gChr);
            return gChr;
        }
        public bool RemoveGameCharacterByCid(string cId)
        {
            if(characterDict.TryRemove(cId, out var _))
            {
                return true;
            }
            return false;
        }
        public GameCharacter GetGameCharacterByCid(string cId)
        {
            if(characterDict.TryGetValue(cId, out var gChr))
            {
                return gChr;
            }
            return null;
        }
    }
}
