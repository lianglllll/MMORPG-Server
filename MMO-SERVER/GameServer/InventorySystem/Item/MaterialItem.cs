using Proto;
using System.Collections;
using System.Collections.Generic;

namespace GameServer.InventorySystem
{

    /// <summary>
    /// 材料
    /// </summary>
    public class MaterialItem : Item
    {
        public MaterialItem(ItemInfo itemInfo) : base(itemInfo)
        {
        }

        public MaterialItem(ItemDefine define, int amount = 1, int position = 0) : base(define, amount, position)
        {
        }
    }

}

