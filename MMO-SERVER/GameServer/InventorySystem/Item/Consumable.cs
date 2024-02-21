using Proto;
using System.Collections;
using System.Collections.Generic;




namespace GameServer.InventorySystem
{
    /// <summary>
    /// 消耗品
    /// </summary>
    public class Consumable : Item
    {
        public Consumable(ItemInfo itemInfo) : base(itemInfo)
        {
        }

        public Consumable(ItemDefine define, int amount = 1, int position = 0) : base(define, amount, position)
        {
        }
    }

}

