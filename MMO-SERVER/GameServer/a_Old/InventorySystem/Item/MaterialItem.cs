using HS.Protobuf.Game.Backpack;

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

