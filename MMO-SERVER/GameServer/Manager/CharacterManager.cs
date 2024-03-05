using FreeSql;
using GameServer.Database;
using GameServer.Model;
using Google.Protobuf;
using Serilog;
using Summer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{


    /// <summary>
    /// 统一管理全部的角色（创建，移除，获取）
    /// </summary>
    public class CharacterManager: Singleton<CharacterManager>
    {
        //游戏中全部的角色<entityid,characterObj>,支持线程安全的字典
        private ConcurrentDictionary<int, Character> characterDict = new ConcurrentDictionary<int, Character>();

        //角色表的数据库对象
        IBaseRepository<DbCharacter> repo = DbManager.fsql.GetRepository<DbCharacter>();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CharacterManager() {
            //计时任务，用于保存角色信息
            //5秒保存一次
            Scheduler.Instance.AddTask(SaveCharacterInfo, 5);
        }

        /// <summary>
        /// 创建一个角色对象，创建的同时，记录在manager中
        /// </summary>
        /// <param name="dbchr"></param>
        /// <returns></returns>
        public Character CreateCharacter(DbCharacter dbchr)
        {
            Character chr = new Character(dbchr);
            EntityManager.Instance.AddEntity(dbchr.SpaceId, chr);
            characterDict[chr.EntityId] = chr;
            return chr;
        }

        /// <summary>
        /// 移除一个角色
        /// </summary>
        /// <param name="chrId"></param>
        public void RemoveCharacter(int entityId)
        {
            //角色列表中删除
            if(characterDict.TryRemove(entityId, out Character chr)){
                //entity列表中删除
                EntityManager.Instance.RemoveEntity(chr.currentSpace.SpaceId, chr.EntityId);
            }
            else
            {
                Log.Error($"[CharacterManager]移除角色失败，没有entityid为{entityId}的角色。");
            }
        }

        /// <summary>
        /// 根据chrid获取一个角色
        /// </summary>
        /// <param name="chrId"></param>
        /// <returns></returns>
        public Character GetCharacter(int entityId)
        {
            return characterDict.GetValueOrDefault(entityId, null);//不存在就返回null
        }

        /// <summary>
        /// 清除角色列表
        /// </summary>
        public void ClearCharacters()
        {
            characterDict.Clear();
        }

        /// <summary>
        /// 遍历角色列表，将位置信息保存到数据库中
        /// </summary>
        private void SaveCharacterInfo()
        {
            foreach (var chr in characterDict.Values)
            {
                //更新位置
                chr.Data.X = chr.Position.x;
                chr.Data.Y = chr.Position.y;
                chr.Data.Z = chr.Position.z;
                chr.Data.Hp = (int)chr.Hp;
                chr.Data.Mp = (int)chr.Mp;
                chr.Data.SpaceId = chr.SpaceId;
                chr.Data.Knapsack = chr.knapsack.InventoryInfo.ToByteArray();
                chr.Data.Level = chr.Level;
                chr.Data.Exp = chr.Exp;
                chr.Data.Gold = chr.Gold;
                chr.Data.EquipsData = chr.equipmentManager.InventoryInfo.ToByteArray();
                //保存进入数据库
                repo.UpdateAsync(chr.Data);//异步更新
            }
        }


    }
}
