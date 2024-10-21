using GameServer.Core;
using GameServer.InventorySystem;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Model
{
    /// <summary>
    /// 物体的实体模型
    /// 场景中显示的模型
    /// </summary>
    public class EItem:Entity
    {
        private Item item;          //item信息
        private NetItemEntity info; //网络对象
        private Space space;

        public Item Item { get { return item; }}
        public NetItemEntity NetItemEntity { get { return info; } }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="item"></param>
        /// <param name="pos"></param>
        /// <param name="dir"></param>
        public EItem(Item item, Space space, Vector3Int pos, Vector3Int dir):base(pos,dir)
        {
            this.item = item;
            this.space = space;

            info = new NetItemEntity();
            info.ItemInfo = item.ItemInfo;
            info.SpaceId = space.SpaceId;
            info.Entity = EntityData;
        }

        /// <summary>
        /// 更新数量
        /// </summary>
        /// <param name="amount"></param>
        public void UpdateAmount(int amount)
        {

        }

    }
}
