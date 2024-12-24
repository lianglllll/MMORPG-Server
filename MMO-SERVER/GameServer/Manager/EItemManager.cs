using Common.Summer.Core;
using GameServer.InventorySystem;
using GameServer.Model;
using System;
using System.Collections.Generic;

namespace GameServer.Manager
{

    /// <summary>
    /// 每个space中都有一个EItemManager，用于管理场景中的item实例
    /// </summary>
    public class EItemManager
    {
        private Space space;
        public Dictionary<int, EItem> itemEntityDict = new Dictionary<int, EItem>();        //<entityid,ItemEntity>


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
        public EItem Create(Item item, Vector3Int pos, Vector3Int dir)
        {
            var eItem = new EItem(item, space, pos, dir);

            //添加到entityMananger中管理
            EntityManager.Instance.AddEntity(space.SpaceId, eItem);

            //添加到当前的mostermanager中管理，分配entityid
            itemEntityDict[eItem.EntityId] = eItem;

            //显示到当前场景
            this.space.EntityJoin(eItem);

            return eItem;
        }
        /// <summary>
        /// 在当前场景移除Item实例
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean RemoveItem(EItem item)
        {
            if (!itemEntityDict.ContainsKey(item.EntityId)) return false;
            EntityManager.Instance.RemoveEntity(space.SpaceId, item.EntityId);
            itemEntityDict.Remove(item.EntityId);

            //场景中移除
            space.EntityLeave(item);

            return true;
        }
        public EItem GetEItemByEntityId(int entityId)
        {
            return itemEntityDict.GetValueOrDefault(entityId);
        }

    }
}
