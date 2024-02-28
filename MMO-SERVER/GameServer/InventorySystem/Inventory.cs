using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using Proto;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.InventorySystem
{

    /// <summary>
    /// 库存对象，可以用作仓库，可以用做背包
    /// </summary>
    public class Inventory
    {
        protected List<Item> itemList = new List<Item>();   
        protected ConcurrentDictionary<int, Item> ItemDict = new ConcurrentDictionary<int, Item>();//物品字典<插槽索引，item>

        private InventoryInfo _inventoryInfo;               //网络对象,主要是用作发送给客户端，和保存数据库
        private bool hasChanged;                            //网络对象是否有变化
        private int capacity;                               //背包/仓库的大小
        private Character chr;                              //归属者

        /// <summary>
        /// 获取网络对象
        /// </summary>
        public InventoryInfo InventoryInfo
        {
            get
            {
                if (_inventoryInfo == null)
                {
                    _inventoryInfo = new InventoryInfo();
                }
                if (hasChanged)
                {
                    _inventoryInfo.Capacity = capacity;
                    _inventoryInfo.List.Clear();
                    foreach(var item in ItemDict.Values)
                    {
                        _inventoryInfo.List.Add(item.ItemInfo);

                    }

                    hasChanged = false;
                }

                return _inventoryInfo;
            }
        }
        public Character Chr { get { return chr; }}
        public int Capacity { get { return capacity; } }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_chr"></param>
        public Inventory(Character _chr)
        {
            hasChanged = false;
            this.chr = _chr;
        }

        /// <summary>
        /// 背包初始化
        /// </summary>
        public void Init(byte[] bytes)
        {
            if(bytes == null)
            {
                capacity = 10;
                hasChanged = true;
                return;
            }

            //数据还原 binary => Inventory
            InventoryInfo inv = InventoryInfo.Parser.ParseFrom(bytes);
            capacity = inv.Capacity;

            //创建物品
            foreach(var iteminfo in inv.List)
            {
                Item item = null;
                var def = DataManager.Instance.ItemDefinedDict[iteminfo.ItemId];
                if(def.ItemType == "消耗品")
                {
                    item = new Consumable(iteminfo);
                }
                else if(def.ItemType == "道具")
                {
                    item = new MaterialItem(iteminfo);
                }else if(def.ItemType == "装备")
                {
                    item = new Equipment(iteminfo);
                }
                else
                {
                    continue;
                }
                //设置到背包中
                SetItem(iteminfo.Position, item);
            }
            hasChanged = true;
        }

        /// <summary>
        /// 设置插槽中的物品，index从0开始
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetItem(int slotIndex,Item item)
        {
            if(slotIndex < 0 || slotIndex >= capacity)
            {
                return false;
            }
            if (item == null) return false;

            hasChanged = true;

            //设置插槽
            ItemDict[slotIndex] = item;
            item.Position = slotIndex;
            return true;
        }

        /// <summary>
        /// 添加物品
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public int AddItem(int itemId,int amount = 1)
        {
            //1.检查物品是否存在
            if (!DataManager.Instance.ItemDefinedDict.TryGetValue(itemId, out var def))
            {
                Log.Information("物品id={0}不存在", itemId);
                return 0;
            }

            //2.检查背包容量是否充足
            Item firstSameItem = null;
            int firstEmptyItemIndex = -1;
            if (CaculateMaxRemainAmount(itemId,ref firstSameItem, ref firstEmptyItemIndex) < amount) return 0;

            var counter = amount;

            //3.循环放置
            while(counter > 0)
            {
                //寻找背包种存放着相同物品的格子
                var sameItem = FindFirstItemByItemIdAndNotFull(itemId);
                if(sameItem != null)
                {
                    //本次可以处理的数量
                    var currentProcessAmount = Math.Min(counter, sameItem.StackingUpperLimit - sameItem.Amount);
                    sameItem.Amount += currentProcessAmount;
                    counter -= currentProcessAmount;
                }
                else
                {
                    //找个空格
                    var emptyIndex = FindEmptyIndex();
                    if(emptyIndex != -1)
                    {
                        //本次可以处理的数量
                        var currentProcessAmount = Math.Min(counter, def.Capicity);
                        var newItem = new Item(def, currentProcessAmount,emptyIndex);
                        SetItem(emptyIndex, newItem);
                        counter -= currentProcessAmount;
                    }
                    else
                    {
                        //没有空格了
                        return amount - counter;
                    }
                }
            }

            //返回成功添加的个数
            return amount - counter;
        }

        /// <summary>
        /// 添加物品，返回值是 0 || 1
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int AddItem(Item item)
        {
            int index = FindEmptyIndex();
            if(index < 0)
            {
                return 0;
            }
            SetItem(index, item);
            return item.Amount;
        }

        /// <summary>
        /// 移除某个格子
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Item removeSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= capacity)
            {
                return null;
            }
            ItemDict.TryRemove(slotIndex, out var _value);
            hasChanged = true;
            return _value;
        }

        /// <summary>
        /// 交互物品位置或者合并
        /// </summary>
        /// <param name="index"></param>
        /// <param name="targetIndex"></param>
        /// <returns></returns>
        public bool Exchange(int originSlotIndex,int targetSlotIndex)
        {
            //两个index的合法性检查
            if (originSlotIndex < 0 || originSlotIndex >= capacity) return false;
            if (targetSlotIndex < 0 || targetSlotIndex >= capacity) return false;

            //1.查找原始插槽物品
            if(!ItemDict.TryGetValue(originSlotIndex,out var originItem))
            {
                return false;
            }

            //2.查找目标插槽物品，如果为空直接放置
            if(!ItemDict.TryGetValue(targetSlotIndex, out var targetItem))
            {
                SetItem(targetSlotIndex, originItem);
                removeSlot(originSlotIndex);
            }

            //3.查找目标物品不为空
            else
            {
                //如果物品类型相同
                if(originItem.ItemId == targetItem.ItemId)
                {
                    int processAmount = Math.Min(targetItem.StackingUpperLimit - targetItem.Amount, originItem.Amount);
                    //原始物品的数量小于等于可操纵数量，将原始物品全部移动到目标物品当中
                    if(originItem.Amount <= processAmount)
                    {
                        targetItem.Amount += originItem.Amount;
                        removeSlot(originSlotIndex);
                    }
                    else
                    {
                        //修改数量即可
                        originItem.Amount -= processAmount;
                        targetItem.Amount += processAmount;
                    }
                }
                //物品类型不同，那就交换
                else
                {
                    SetItem(originSlotIndex, targetItem);
                    SetItem(targetSlotIndex, originItem);
                }
            }

            hasChanged = true;
            return true;
        }

        /// <summary>
        /// 移除指定数量的物品
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public int RemoveItem(int itemId,int amount = 1)
        {
            //1.检查物品是否存在
            if (!DataManager.Instance.ItemDefinedDict.TryGetValue(itemId, out var def))
            {
                Log.Information("物品id={0}不存在", itemId);
                return 0;
            }

            int removedAmount = 0;
            while(amount > 0)
            {
                var item = FindFirstItemByItemId(itemId);
                if(item == null)
                {
                    break;
                }

                //判断要移除的数量是否大于物品当前的数量
                int curAmount = Math.Min(amount, item.Amount);
                item.Amount -= curAmount;
                removedAmount += curAmount;
                amount -= curAmount;
                //没了就清空
                if(item.Amount == 0)
                {
                    removeSlot(item.Position);
                }
            }
            
            if(removedAmount > 0)
            {
                hasChanged = true;
            }

            return removedAmount;

        }

        /// <summary>
        /// 丢弃指定数量的物品
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public int Discard(int slotIndex,int amount = 1)
        {
            //安全校验
            if (slotIndex < 0 || slotIndex >= capacity)
            {
                return 0;
            }
            if(amount <= 0)
            {
                return 0;
            }
            if(!ItemDict.TryGetValue(slotIndex,out var item))
            {
                return 0;
            }

            hasChanged = true;

            //丢弃一部分
            if (amount < item.Amount)
            {
                item.Amount -= amount;
                var newItem = new Item(item.Define, amount);
                Chr.currentSpace.itemManager.Create(newItem, Chr.Position,Vector3Int.zero);
                return amount;
            }

            //丢弃全部
            removeSlot(slotIndex);
            int tmpAmount = item.Amount;
            Chr.currentSpace.itemManager.Create(item, Chr.Position, Vector3Int.zero);
            return tmpAmount;
        }

        /// <summary>
        /// 检查当前背包还可以放多少个Itemid这种物品,顺便找到第一个sameItem和第一个空格子
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private int CaculateMaxRemainAmount(int itemId, ref Item firstSameItem, ref int firstEmptyItemIndex)
        {
            //1.检查物品是否存在
            if (!DataManager.Instance.ItemDefinedDict.TryGetValue(itemId, out var def))
            {
                Log.Information("物品id={0}不存在", itemId);
                return 0;
            }

            //记录可用数量
            var amounter = 0;
            for (int i = 0; i < capacity; ++i)
            {
                if (ItemDict.TryGetValue(i, out var item))
                {
                    if (item.ItemId == itemId)
                    {
                        amounter += (item.StackingUpperLimit - item.Amount);
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
        /// 寻找一个空的背包索引
        /// </summary>
        /// <returns></returns>
        private int FindEmptyIndex()
        {
            for(int i = 0;i < capacity; ++i)
            {
                if(!ItemDict.TryGetValue(i,out var item))
                {
                    return i;
                }
                
            }
            return -1;
        }

        /// <summary>
        /// 寻找相同的item并且没有达到堆叠上限
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private Item FindFirstItemByItemIdAndNotFull(int itemId)
        {
            return ItemDict.Values.FirstOrDefault(item => item.Define.ID == itemId && item.Amount < item.StackingUpperLimit);
        }

        /// <summary>
        /// 通过itemid寻找Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private Item FindFirstItemByItemId(int itemId)
        {
            return ItemDict.Values.FirstOrDefault(item => item.Define.ID == itemId);
        }

        /// <summary>
        /// 寻找背包中的物品
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <returns></returns>
        public Item GetItemBySlotIndex(int slotIndex)
        {
            return ItemDict.GetValueOrDefault(slotIndex, null);
        }
    }
}
