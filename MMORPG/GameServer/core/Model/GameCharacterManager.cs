using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using System.Collections.Concurrent;

namespace GameServer.Core.Model
{
    public class GameCharacterManager : Singleton<GameCharacterManager>
    {
        private ConcurrentDictionary<string, GameCharacter> characterDict = new();
        private ConcurrentDictionary<int, ConcurrentDictionary<string, GameCharacter>> sceneGrounpChr = new();

        public void Init()
        {
        }
        public void UnInit()
        {
            characterDict.Clear();
            sceneGrounpChr.Clear();
        }

        public GameCharacter CreateGameCharacter(DBCharacterNode dbChr)
        {
            var gChr = new GameCharacter(dbChr);
            
            characterDict.TryAdd(dbChr.CId, gChr);

            if (!sceneGrounpChr.ContainsKey(gChr.CurSceneId))
            {
                sceneGrounpChr.TryAdd(gChr.CurSceneId, new ConcurrentDictionary<string, GameCharacter>());
            }
            sceneGrounpChr[gChr.CurSceneId].TryAdd(gChr.Cid, gChr);

            return gChr;
        }
        public bool RemoveGameCharacterByCid(string cId, HS.Protobuf.Scene.CharacterLeaveSceneResponse message)
        {
            bool result = false;
            if(!characterDict.TryRemove(cId, out var chr))
            {
                goto End;
            }
            sceneGrounpChr[chr.CurSceneId].TryRemove(cId, out var _);
            chr.SaveGameCharacter(message);
        End:
            return result;
        }
        public GameCharacter GetGameCharacterByCid(string cId)
        {
            if(characterDict.TryGetValue(cId, out var gChr))
            {
                return gChr;
            }
            return null;
        }

        public ConcurrentDictionary<string, GameCharacter> GetAllGameCharacter()
        {
            return characterDict;
        }
        public ConcurrentDictionary<string, GameCharacter> GetPartGameCharacterBySceneId(int sceneId)
        {
            sceneGrounpChr.TryGetValue(sceneId, out var chrs);
            return chrs;
        }
    }
}
