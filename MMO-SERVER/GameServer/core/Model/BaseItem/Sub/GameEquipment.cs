using GameServer.Combat;
using HS.Protobuf.Game.Backpack;

namespace GameServer.core.Model.BaseItem.Sub
{
    /// <summary>
    /// 装备
    /// </summary>
    public class GameEquipment : GameItem
    {
        public EquipsType EquipsType => ParseEquipType(m_itemDefine.EquipType);
        public AttrubuteData attrubuteData;

        public GameEquipment(NetItemDataNode netItemDataNode) : base(netItemDataNode)
        {
            LoadAttrubuteData();
        }
        public GameEquipment(ItemDefine define,int position = 0) : base(define, 1, position)
        {
            LoadAttrubuteData();
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

        private void LoadAttrubuteData()
        {
            attrubuteData = new AttrubuteData
            {
                Speed   = m_itemDefine.Speed,
                HPMax   = m_itemDefine.HP,
                MPMax   = m_itemDefine.MP,
                AD      = m_itemDefine.AD,
                AP      = m_itemDefine.AP,
                DEF     = m_itemDefine.DEF,
                MDEF    = m_itemDefine.MDEF,
                CRI     = m_itemDefine.CRI,
                CRD     = m_itemDefine.CRD,
                STR     = m_itemDefine.STR,
                INT     = m_itemDefine.INT,
                AGI     = m_itemDefine.AGI,
                HitRate = m_itemDefine.HitRate,
                DodgeRate   = m_itemDefine.DodgeRate,
                HpRegen     = m_itemDefine.HpRegen,
                HpSteal     = m_itemDefine.HpSteal,
            };
        }

    }

}

