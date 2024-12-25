using HS.Protobuf.Game.Backpack;

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

