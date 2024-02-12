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

            //添加到entity中管理
            EntityManager.Instance.AddEntity(space.SpaceId, ie);

            //添加到当前的mostermanager中管理
            itemEntityDict[ie.EntityId] = ie;

            //显示到当前场景
            this.space.ItemJoin(ie);

            return ie;
        }

        public Boolean RemoveItem(int entityId)
        {
            if (!itemEntityDict.ContainsKey(entityId)) return false;
            itemEntityDict.Remove(entityId);
            //entitymanager remove
            EntityManager.Instance.RemoveEntity(space.SpaceId, entityId);
            return true;
        }


    }
}
