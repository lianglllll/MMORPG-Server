using HS.Protobuf.Game.Backpack;
using System;

namespace GameServer.InventorySystem
{
    /// <summary>
    /// 物品基类
    /// </summary>
    [Serializable]
    public abstract class Item 
    {
        public ItemDefine Define { get; set; } 
        protected ItemInfo _itmeInfo;                               //网络对象

        public int ItemId
        {
            get
            {
                return Define.ID;
            }
        }
        public int Amount
        {
            get { return _itmeInfo.Amount; }
            set { _itmeInfo.Amount = value; }
        }
        public int Position
        {
            get
            {
                return _itmeInfo.Position;
            }
            set
            {
                _itmeInfo.Position = value;
            }
        }
        public int StackingUpperLimit
        {
            get
            {
                return Define.Capicity;
            }
        }                           //堆叠上限
        public ItemInfo ItemInfo
        {
            get
            {
                return _itmeInfo;
            }
        }

        /// <summary>
        /// 构造方法,network创建
        /// </summary>
        /// <param name="itemInfo"></param>
        public Item(ItemInfo itemInfo)
        {
            this.Define = DataManager.Instance.ItemDefinedDict[itemInfo.ItemId];
            this._itmeInfo = itemInfo;
        }

        /// <summary>
        /// 构造方法，添加用
        /// </summary>
        /// <param name="define"></param>
        public Item(ItemDefine define,int amount = 1,int position = 0)
        {
            Define = define;
            _itmeInfo = new ItemInfo() { ItemId = Define.ID };
            this._itmeInfo.Amount = amount;
            this._itmeInfo.Position = position;
        }

        /// <summary>
        /// 获取item的类型
        /// </summary>
        /// <returns></returns>
        public ItemType GetItemType()
        {
            switch (Define.ItemType)
            {
                case "消耗品": return ItemType.Consumable;
                case "道具": return ItemType.Material;
                case "装备": return ItemType.Equipment;
            }
            return ItemType.Consumable;
        }

        /// <summary>
        /// 获取item的品质
        /// </summary>
        /// <returns></returns>
        public Quality GetQuality()
        {
            switch (Define.Quality)
            {
                case "普通": return Quality.Common;
                case "非凡": return Quality.Fine;
                case "稀有": return Quality.Rare;
                case "史诗": return Quality.Epic;
                case "传说": return Quality.Legendary;
                case "神器": return Quality.Artifact;
            }
            return Quality.Common;
        }

        /// <summary>
        /// 创建Item实例
        /// </summary>
        /// <param name="def"></param>
        /// <param name="amount"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Item CreateItem(ItemDefine def,int amount,int pos)
        {
            Item newItem = null;
            switch (def.ItemType)
            {
                case "消耗品":
                    newItem = new Consumable(def, amount, pos);
                    break;
                case "道具":
                    newItem = new MaterialItem(def, amount, pos);
                    break;
                case "装备":
                    newItem = new Equipment(def);
                    break;
            }
            return newItem;
        }
        public static Item CreateItem(ItemInfo info)
        {
            var def = DataManager.Instance.ItemDefinedDict[info.ItemId];
            if (def == null) return null;
            return Item.CreateItem(def, info.Amount, info.Position);
        }

    }

}

