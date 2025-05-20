using Common.Summer.Core;
using GameServer.core.Model.BaseItem;
using GameServer.core.Model.BaseItem.Sub;
using GameServer.Core.Model;
using GameServer.Utils;
using HS.Protobuf.Backpack;
using HS.Protobuf.Scene;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace GameServer.InventorySystem
{
    // 库存对象，可以用作仓库，可以用做背包
    public class InventoryManager
    {
        private GameCharacter m_owner;                                  // 归属者
        protected ConcurrentDictionary<int, GameItem> ItemDict = new(); // 物品字典<插槽索引，item>
        private NetItemInventoryDataNode m_netItemInventoryDataNode;    // 网络对象,主要是用作发送给客户端，和保存数据库
        private bool m_hasChange;
        private bool m_netDataIsNewly;

        #region GetSet
        public bool HasChanged
        {
            get => m_hasChange;
            set
            {
                m_hasChange = value;
                m_netDataIsNewly = false;
            }
        }
        public NetItemInventoryDataNode NetItemInventoryDataNode
        {
            get
            {
                if (!m_netDataIsNewly)
                {
                    m_netItemInventoryDataNode.ItemDataNodes.Clear();
                    foreach (var item in ItemDict.Values)
                    {
                        m_netItemInventoryDataNode.ItemDataNodes.Add(item.NetItemDataNode);
                    }
                    m_netDataIsNewly = true;
                }
                return m_netItemInventoryDataNode;
            }
        }
        public GameCharacter Chr => m_owner;
        public int Capacity => m_netItemInventoryDataNode.Capacity;
        #endregion
        #region 生命周期
        public void Init(GameCharacter chr, ItemInventoryType inventoryType, byte[] bytes)
        {
            m_owner = chr;

            // 信息为空
            if (bytes == null || bytes.Count() == 0)
            {
                m_netItemInventoryDataNode = new();
                m_netItemInventoryDataNode.InventoryType = inventoryType;
                m_netItemInventoryDataNode.Capacity = 20;
                HasChanged = true;
                goto End;
            }

            // 数据还原 binary => Inventory
            m_netItemInventoryDataNode = NetItemInventoryDataNode.Parser.ParseFrom(bytes);

            //创建物品
            foreach(var itemNode in m_netItemInventoryDataNode.ItemDataNodes)
            {
                GameItem item = _CreateItem(itemNode);
                ItemDict[itemNode.GridIdx] = item;
            }

            HasChanged = false;
            m_netDataIsNewly = false;
        End:
            return;
        }
        #endregion
        #region 背包操作函数
        public int AddGameItem(int itemId, int count = 1)
        {
            int counter = count;
            // 1.检查物品是否存在
            if (!StaticDataManager.Instance.ItemDefinedDict.TryGetValue(itemId, out var def))
            {
                Log.Warning("物品id={0}不存在", itemId);
                goto End;
            }

            // 2.检查背包容量是否充足
            GameItem firstSameItem = null;
            int firstEmptyItemIndex = -1;
            if (_CaculateMaxRemainCount(itemId, ref firstSameItem, ref firstEmptyItemIndex) < count)
            {
                goto End;
            }

            // 3.循环放置
            while (counter > 0)
            {
                // 寻找背包种存放着相同物品的格子
                var sameItem = _FindFirstItemByItemIdAndNotFull(itemId);
                if (sameItem != null)
                {
                    //本次可以处理的数量
                    var currentProcessAmount = Math.Min(counter, sameItem.StackingUpperLimit - sameItem.Count);
                    sameItem.Count += currentProcessAmount;
                    counter -= currentProcessAmount;
                }
                else
                {
                    //找个空格
                    var emptyIndex = _FindEmptyIndex();
                    if (emptyIndex != -1)
                    {
                        //本次可以处理的数量
                        var currentProcessCount = Math.Min(counter, def.Capicity);
                        GameItem newItem = _CreateItem(def, currentProcessCount, emptyIndex);
                        ItemDict[emptyIndex] = newItem;
                        counter -= currentProcessCount;
                    }
                    else
                    {
                        //没有空格了
                        break;
                    }
                }
            }

            HasChanged = true;

        End:
            // 返回成功添加的个数
             return count - counter;
        }
        public bool AddGameEquipItem(GameItem item)
        {
            bool result = false;
            int newGridIdx = _FindEmptyIndex();
            if (newGridIdx < 0)
            {
                goto End;
            }
            ItemDict[newGridIdx] = item;

            HasChanged = true;

            result = true;
        End:
            return result;
        }
        public bool AddGameItemToTargetGrid(GameItem gameItem, int targetGridIdx)
        {
            bool result = false;
            if (ItemDict.ContainsKey(targetGridIdx))
            {
                goto End;
            }
            ItemDict.TryAdd(targetGridIdx, gameItem);
            gameItem.GridIdx = targetGridIdx;

            HasChanged = true;

        End:
            return result;
        }
        public GameItem GetItemByGridIdx(int gridIdx)
        {
            return ItemDict.GetValueOrDefault(gridIdx, null);
        }
        public GameItem RemoveGrid(int gridIdx)
        {
            if (gridIdx < 0 || gridIdx >= Capacity)
            {
                return null;
            }
            ItemDict.TryRemove(gridIdx, out var value);
            
            HasChanged = true;
            
            return value;
        }
        public int RemoveItem(int itemId, int count = 1)
        {
            // 1.检查物品是否存在
            if (!StaticDataManager.Instance.ItemDefinedDict.TryGetValue(itemId, out var def))
            {
                //Log.Information("物品id={0}不存在", itemId);
                return 0;
            }

            int removedAmount = 0;
            while (count > 0)
            {
                var item = _FindFirstItemByItemId(itemId);
                if (item == null)
                {
                    break;
                }

                // 判断要移除的数量是否大于物品当前的数量
                int curAmount = Math.Min(count, item.Count);
                item.Count -= curAmount;
                removedAmount += curAmount;
                count -= curAmount;
                // 没了就清空
                if (item.Count == 0)
                {
                    RemoveGrid(item.GridIdx);
                }
            }

            if (removedAmount > 0)
            {
                HasChanged = true;
            }

            return removedAmount;

        }
        public int Discard(int girdIdx, int count = 1)
        {
            int result = 0;

            if (count <= 0)
            {
                goto End;
            }
            if (!ItemDict.TryGetValue(girdIdx, out var item))
            {
                goto End;
            }

            // 丢弃一部分
            if (count < item.Count)
            {
                item.Count -= count;
                GameItem newItem = _CreateItem(item.ItemDefine, count, 0);
                _SendDiscardGameItemToSceneRequest(newItem);
                result = count;
            }
            else
            {
                // 丢弃全部
                RemoveGrid(girdIdx);
                int tmpAmount = item.Count;
                _SendDiscardGameItemToSceneRequest(item);
                result = tmpAmount;
            }

            HasChanged = true;

        End:
            return result;
        }
        public bool UseItem(int gridIdx, int count)
        {
            bool result = false;
            if(!ItemDict.TryGetValue(gridIdx, out var gameItem))
            {
                goto End;
            }
            if(gameItem.Count < count)
            {
                goto End;
            }

            gameItem.Count -= count;
            if(gameItem.Count == 0)
            {
                RemoveGrid(gridIdx);
            }

            // 具体item使用的调用代码
            // todo

            HasChanged = true;

        End:
            return result;
        }
        #endregion
        #region tools


        /// <summary>
        /// 检查当前背包还可以放多少个Itemid这种物品,顺便找到第一个sameItem和第一个空格子
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private int _CaculateMaxRemainCount(int itemId, ref GameItem firstSameItem, ref int firstEmptyItemIndex)
        {
            var def = StaticDataManager.Instance.ItemDefinedDict[itemId];
            // 记录可用数量
            var amounter = 0;
            for (int i = 0; i < Capacity; ++i)
            {
                if (ItemDict.TryGetValue(i, out var item))
                {
                    if (item.ItemId == itemId)
                    {
                        amounter += (item.StackingUpperLimit - item.Count);
                        if (firstSameItem == null)
                        {
                            firstSameItem = item;
                        }
                    }
                }
                else
                {
                    amounter += def.Capicity;
                    if (firstEmptyItemIndex == -1)
                    {
                        firstEmptyItemIndex = i;
                    }
                }
            }

            return amounter;
        }
        /// <summary>
        /// 寻找相同的item并且没有达到堆叠上限
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private GameItem _FindFirstItemByItemIdAndNotFull(int itemId)
        {
            return ItemDict.Values.FirstOrDefault(item => item.ItemDefine.ID == itemId && item.Count < item.StackingUpperLimit);
        }
        /// <summary>
        /// 通过itemid寻找Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private GameItem _FindFirstItemByItemId(int itemId)
        {
            return ItemDict.Values.FirstOrDefault(item => item.ItemDefine.ID == itemId);
        }
        /// <summary>
        /// 寻找一个空的背包索引
        /// </summary>
        /// <returns></returns>
        private int _FindEmptyIndex()
        {
            for (int i = 0; i < Capacity; ++i)
            {
                if (!ItemDict.TryGetValue(i, out var item))
                {
                    return i;
                }
            }
            return -1;
        }
        /// <summary>
        /// 交互物品位置或者合并
        /// </summary>
        /// <param name="index"></param>
        /// <param name="targetIndex"></param>
        /// <returns></returns>
        public bool ExchangeGridOrMergeGrid(int originSlotIndex, int targetSlotIndex)
        {
            // 两个index的合法性检查
            if (originSlotIndex < 0 || originSlotIndex >= Capacity) return false;
            if (targetSlotIndex < 0 || targetSlotIndex >= Capacity) return false;

            // 1.查找原始插槽物品
            if (!ItemDict.TryGetValue(originSlotIndex, out var originItem))
            {
                return false;
            }

            // 2.查找目标插槽物品，如果为空直接放置
            HasChanged = true;
            if (!ItemDict.TryGetValue(targetSlotIndex, out var targetItem))
            {
                _ReSetItemGridIdx(targetSlotIndex, originItem);
                RemoveGrid(originSlotIndex);
            }

            // 3.查找目标物品不为空
            else
            {
                // 如果物品相同
                if (originItem.ItemId == targetItem.ItemId)
                {
                    int processAmount = Math.Min(targetItem.StackingUpperLimit - targetItem.Count, originItem.Count);
                    // 原始物品的数量小于等于可操纵数量，将原始物品全部移动到目标物品当中
                    if (originItem.Count <= processAmount)
                    {
                        targetItem.Count += originItem.Count;
                        RemoveGrid(originSlotIndex);
                    }
                    else
                    {
                        // 修改数量即可
                        originItem.Count -= processAmount;
                        targetItem.Count += processAmount;
                    }
                }
                // 物品类型不同，那就交换
                else
                {
                    _ReSetItemGridIdx(originSlotIndex, targetItem);
                    _ReSetItemGridIdx(targetSlotIndex, originItem);
                }
            }

            return true;
        }

        public bool IsCanAddToInventory(int targetItemId, int targetCount)
        {
            bool result = false;
            int counter = 0;
            var itemDef = StaticDataManager.Instance.ItemDefinedDict[targetItemId];
            for (int i = 0; i < Capacity; ++i)
            {
                if (ItemDict.TryGetValue(i, out var item))
                {
                    if (item.ItemId == targetItemId)
                    {
                        counter += (item.StackingUpperLimit - item.Count);
                    }
                }
                else
                {
                    counter += itemDef.Capicity;
                }
                if (counter >= targetCount)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        private GameItem _CreateItem(NetItemDataNode netItemDataNode)
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
        public GameItem _CreateItem(ItemDefine def, int amount, int pos)
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
                    newItem = new GameEquipment(def, pos);
                    break;
            }
            return newItem;
        }
        private bool _ReSetItemGridIdx(int gridIdx, GameItem item)
        {
            bool result = false;
            if (gridIdx < 0 || gridIdx >= Capacity || item == null)
            {
                goto End;
            }

            // 设置插槽
            ItemDict[gridIdx] = item;
            item.GridIdx = gridIdx;

            HasChanged = true;

            result = true;
        End:
            return result;
        }
        private void _SendDiscardGameItemToSceneRequest(GameItem gameItem)
        {
            var req = new DiscardGameItemToSceneRequest();
            req.EntityId = m_owner.EntityId;
            req.ItemDataNode = gameItem.NetItemDataNode;
            m_owner.SendToScene(req);
        }
        #endregion
    }
}
