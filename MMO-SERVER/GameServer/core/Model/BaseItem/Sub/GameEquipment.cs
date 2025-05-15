using HS.Protobuf.Backpack;
using HS.Protobuf.SceneEntity;

namespace GameServer.core.Model.BaseItem.Sub
{
    /// <summary>
    /// 装备
    /// </summary>
    public class GameEquipment : GameItem
    {
        private NetAttrubuteDataNode m_netAttrubuteDataNode;
        private EquipsType m_equipsType;

        #region GetSet
        public EquipsType EquipsType => m_equipsType;
        public NetAttrubuteDataNode NetAttrubuteDataNode => m_netAttrubuteDataNode;
        #endregion

        public GameEquipment(NetItemDataNode netItemDataNode) : base(netItemDataNode)
        {
            m_equipsType = _ParseEquipType(m_itemDefine.EquipType);
            _LoadAttrubuteData();
        }
        public GameEquipment(ItemDefine define, int position = 0) : base(define, 1, position)
        {
            m_equipsType = _ParseEquipType(m_itemDefine.EquipType);
            _LoadAttrubuteData();
        }

        private void _LoadAttrubuteData()
        {
            m_netAttrubuteDataNode = new NetAttrubuteDataNode()
            {
                Speed       = m_itemDefine.Speed,
                MaxHP       = (int)m_itemDefine.HP,
                MaxMP       = (int)m_itemDefine.MP,
                AD          = (int)m_itemDefine.AD,
                AP          = (int)m_itemDefine.AP,
                DEF         = (int)m_itemDefine.DEF,
                MDEF        = (int)m_itemDefine.MDEF,
                CRI         = m_itemDefine.CRI,
                CRD         = m_itemDefine.CRD,
                HitRate     = m_itemDefine.HitRate,
                DodgeRate   = m_itemDefine.DodgeRate,
                HpRegen     = (int)m_itemDefine.HpRegen,
                HpSteal     = m_itemDefine.HpSteal,
                STR         = (int)m_itemDefine.STR,
                INT         = (int)m_itemDefine.INT,
                AGI         = (int)m_itemDefine.AGI,
            };
        }
        private EquipsType _ParseEquipType(string value)
        {
            switch (value)
            {
                case "武器":
                    return EquipsType.Weapon;
                case "头盔":
                    return EquipsType.Helmet;
                case "项链":
                    return EquipsType.Neck;
                case "胸甲":
                    return EquipsType.Chest;
                case "护腕":
                    return EquipsType.Wristband;
                case "手镯":
                    return EquipsType.Bracelet;
                case "戒指":
                    return EquipsType.Ring;
                case "腰带":
                    return EquipsType.Belt;
                case "裤子":
                    return EquipsType.Legs;
                case "鞋子":
                    return EquipsType.Boots;
                case "翅膀":
                    return EquipsType.Wings;
                default:
                    return EquipsType.Unset;
            }
        }
    }
}

