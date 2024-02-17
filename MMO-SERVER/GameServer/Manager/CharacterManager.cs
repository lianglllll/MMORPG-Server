using FreeSql;
using GameServer.Database;
using GameServer.Model;
using Google.Protobuf;
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
        //游戏中全部的角色<characterId,characterObj>,支持线程安全的字典
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
            characterDict[chr.Id] = chr;
            return chr;
        }

        /// <summary>
        /// 移除一个角色
        /// </summary>
        /// <param name="chrId"></param>
        public void RemoveCharacter(int chrId)
        {
            Character chr;
            if(characterDict.TryRemove(chrId,out chr)){
                EntityManager.Instance.RemoveEntity(chr.currentSpace.SpaceId, chr.EntityId);
            }
        }

        /// <summary>
        /// 根据chrid获取一个角色
        /// </summary>
        /// <param name="chrId"></param>
        /// <returns></returns>
        public Character GetCharacter(int chrId)
        {
            return characterDict.GetValueOrDefault(chrId, null);//不存在就返回null
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
                //保存进入数据库
                repo.UpdateAsync(chr.Data);//异步更新
            }
        }


    }
}
