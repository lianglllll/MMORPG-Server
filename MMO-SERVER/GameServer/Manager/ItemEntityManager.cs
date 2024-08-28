using GameServer.Core;
using GameServer.InventorySystem;
using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{

    /// <summary>
    /// 每个space中都有一个ItemEntityManager，用于管理场景中的item实例
    /// </summary>
    public class ItemEntityManager
    {
        private Space space;
        public Dictionary<int, ItemEntity> itemEntityDict = new Dictionary<int, ItemEntity>();        //<entityid,ItemEntity>


        public void Init(Space space)
        {
            this.space = space;
        }

        /// <summary>
        /// 在当前场景创建Item实例
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pos"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public ItemEntity Create(Item item, Vector3Int pos, Vector3Int dir)
        {
            var ie = new ItemEntity(item, space, pos, dir);

            //添加到entityMananger中管理
            EntityManager.Instance.AddEntity(space.SpaceId, ie);

            //添加到当前的mostermanager中管理，分配entityid
            itemEntityDict[ie.EntityId] = ie;

            //显示到当前场景
            this.space.EntityJoin(ie);

            return ie;
        }
        /// <summary>
        /// 在当前场景移除Item实例
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean RemoveItem(ItemEntity item)
        {
            if (!itemEntityDict.ContainsKey(item.EntityId)) return false;
            EntityManager.Instance.RemoveEntity(space.SpaceId, item.EntityId);
            itemEntityDict.Remove(item.EntityId);

            //场景中移除
            space.EntityLeave(item);

            return true;
        }
        public ItemEntity GetItemEntityByEntityId(int entityId)
        {
            return itemEntityDict.GetValueOrDefault(entityId);
        }

    }
}
