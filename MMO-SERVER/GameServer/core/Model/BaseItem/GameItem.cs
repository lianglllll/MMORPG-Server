using GameServer.Utils;
using HS.Protobuf.Game.Backpack;
using HS.Protobuf.SceneEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core.Model.BaseItem
{
    public class GameItem
    {
        protected ItemDefine m_itemDefine;
        protected NetItemDataNode m_netItemDataNode;

        #region GetSet

        public int ItemId => m_itemDefine.ID;
        public int Amount
        {
            get => m_netItemDataNode.Amount; 
            set { m_netItemDataNode.Amount = value; }
        }
        public int GridIdx
        {
            get => m_netItemDataNode.GridIdx;
            set
            {
                m_netItemDataNode.GridIdx = value;
            }
        }
        public int StackingUpperLimit => m_itemDefine.Capicity;
        public NetItemDataNode NetItemDataNode => m_netItemDataNode;

        #endregion

        public GameItem(NetItemDataNode netItemDataNode)
        {
            this.m_itemDefine = StaticDataManager.Instance.ItemDefinedDict[netItemDataNode.ItemId];
            this.m_netItemDataNode = netItemDataNode;
        }
        public GameItem(ItemDefine define, int amount = 1, int gridIdx = 0)
        {
            m_itemDefine = define;
            m_netItemDataNode = new NetItemDataNode() { ItemId = m_itemDefine.ID };
            this.m_netItemDataNode.Amount = amount;
            this.m_netItemDataNode.GridIdx = gridIdx;
        }

        public ItemType GetItemType()
        {
            switch (m_itemDefine.ItemType)
            {
                case "消耗品": return ItemType.Consumable;
                case "道具": return ItemType.Material;
                case "装备": return ItemType.Equipment;
            }
            return ItemType.Consumable;
        }
        public ItemQuality GetItemQuality()
        {
            switch (m_itemDefine.Quality)
            {
                case "普通": return ItemQuality.Common;
                case "非凡": return ItemQuality.Fine;
                case "稀有": return ItemQuality.Rare;
                case "史诗": return ItemQuality.Epic;
                case "传说": return ItemQuality.Legendary;
                case "神器": return ItemQuality.Artifact;
            }
            return ItemQuality.Common;
        }
    }
}
