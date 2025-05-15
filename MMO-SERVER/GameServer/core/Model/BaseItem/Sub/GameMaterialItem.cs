using HS.Protobuf.Backpack;

namespace GameServer.core.Model.BaseItem.Sub
{

    /// <summary>
    /// 材料
    /// </summary>
    public class GameMaterialItem : GameItem
    {
        public GameMaterialItem(NetItemDataNode netItemDataNode) : base(netItemDataNode)
        {
        }

        public GameMaterialItem(ItemDefine define, int amount = 1, int position = 0) : base(define, amount, position)
        {
        }
    }

}

