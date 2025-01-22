using Serilog;
using System.Collections.Concurrent;
using Common.Summer.Core;
using SceneServer.Core.Model.Actor;
using HS.Protobuf.DBProxy.DBCharacter;

namespace SceneServer.Core.Scene
{
    /// <summary>
    /// 统一管理全部的角色（创建，移除，获取）
    /// </summary>
    public class SceneCharacterManager
    {
        //游戏中全部的角色<entityid,characterObj>,支持线程安全的字典
        private ConcurrentDictionary<int, SceneCharacter> characterDict = new();

        public bool Init() {
            ClearCharacters();
            // 5秒保存一次，用于保存角色信息
            Scheduler.Instance.AddTask(_SaveCharacterInfoToDB, 5);
            return true;
        }
        public SceneCharacter CreateSceneCharacter(string sessionId, Connection gameGateConn,DBCharacterNode dbChrNode)
        {
            var chr = new SceneCharacter();
            chr.Init(sessionId, gameGateConn, dbChrNode);
            SceneEntityManager.Instance.AddSceneEntity(chr);
            characterDict[chr.EntityId] = chr;
            return chr;
        }
        public bool RemoveSceneCharacter(int entityId)
        {
            //角色列表中删除
            if(characterDict.TryRemove(entityId, out SceneCharacter chr)){
                //entity列表中删除
                SceneEntityManager.Instance.RemoveSceneEntity(chr.EntityId);
                return true;
            }
            else
            {
                Log.Error($"[CharacterManager]移除角色失败，没有entityid为{entityId}的角色。");
                return false;
            }
        }
        public SceneCharacter GetSceneCharacterByEntityId(int entityId)
        {
            return characterDict.GetValueOrDefault(entityId, null);
        }
        public bool ClearCharacters()
        {
            foreach(var chr in characterDict.Values)
            {
                SceneEntityManager.Instance.RemoveSceneEntity(chr.EntityId);
            }
            characterDict.Clear();
            return true;
        }
        private void _SaveCharacterInfoToDB()
        {
            //foreach (var chr in characterDict.Values)
            //{
            //    //更新位置
            //    chr.Data.X = chr.Position.x;
            //    chr.Data.Y = chr.Position.y;
            //    chr.Data.Z = chr.Position.z;
            //    chr.Data.Hp = (int)chr.Hp;
            //    chr.Data.Mp = (int)chr.Mp;
            //    chr.Data.SpaceId = chr.CurSpaceId;
            //    chr.Data.Knapsack = chr.knapsack.InventoryInfo.ToByteArray();
            //    chr.Data.Level = chr.Level;
            //    chr.Data.Exp = chr.Exp;
            //    chr.Data.Gold = chr.Gold;
            //    chr.Data.EquipsData = chr.equipmentManager.InventoryInfo.ToByteArray();
            //    //保存进入数据库
            //    repo.UpdateAsync(chr.Data);//异步更新
            //}
        }
    }
}
