using Proto;
using System.Collections;
using System.Collections.Generic;

namespace GameServer.InventorySystem
{
    /// <summary>
    /// 装备
    /// </summary>
    public class Equipment : Item
    {
        public EquipsType EquipsType => ParseEquipType(Define.EquipType);

        //network
        public Equipment(ItemInfo itemInfo) : base(itemInfo)
        {
        }

        //define
        public Equipment(ItemDefine define,int position = 0) : base(define, 1, position)
        {
        }

        /// <summary>
        /// str -> enum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private EquipsType ParseEquipType(string value)
        {
            switch (value)
            {
                case "无":
                    return EquipsType.Unset;
                case "武器":
                    return EquipsType.Weapon;
                case "胸甲":
                    return EquipsType.Chest;
                case "腰带":
                    return EquipsType.Belt;
                case "裤子":
                    return EquipsType.Legs;
                case "鞋子":
                    return EquipsType.Boots;
                case "戒指":
                    return EquipsType.Ring;
                case "项链":
                    return EquipsType.Neck;
                case "翅膀":
                    return EquipsType.Wings;
                default:
                    return EquipsType.Unset;
            }
        }


    }

}

