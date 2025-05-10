using Common.Summer.Tools;
using GameServer.core.Model.BaseItem.Sub;
using HS.Protobuf.Game.Backpack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core.Model.BaseItem
{
    public class GameItemManager : Singleton<GameItemManager>
    {
        public static GameItem CreateItem(ItemDefine def, int amount, int pos)
        {
            GameItem newItem = null;
            switch (def.ItemType)
            {
                case "消耗品":
                    newItem = new GameConsumable(def, amount, pos);
                    break;
                case "道具":
                    newItem = new GameMaterialItem(def, amount, pos);
                    break;
                case "装备":
                    newItem = new GameEquipment(def);
                    break;
            }
            return newItem;
        }
        public static GameItem CreateItem(NetItemDataNode netItemDataNode)
        {
            GameItem newItem = null;
            switch (netItemDataNode.ItemType)
            {
                case ItemType.Consumable:
                    newItem = new GameConsumable(netItemDataNode);
                    break;
                case ItemType.Material:
                    newItem = new GameMaterialItem(netItemDataNode);
                    break;
                case ItemType.Equipment:
                    newItem = new GameEquipment(netItemDataNode);
                    break;
            }
            return newItem;
        }

    }
}
