using GameServer.core.Model.BaseItem;
using HS.Protobuf.Game.Backpack;

namespace GameServer.core.Model.BaseItem.Sub
{
    /// <summary>
    /// 消耗品
    /// </summary>
    public class GameConsumable : GameItem
    {
        public GameConsumable(NetItemDataNode netItemDataNode) : base(netItemDataNode)
        {
        }

        public GameConsumable(ItemDefine define, int amount = 1, int position = 0) : base(define, amount, position)
        {
        }
    }

}

