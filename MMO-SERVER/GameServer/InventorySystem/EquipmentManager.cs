using GameServer.Model;
using Proto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.InventorySystem
{
    /// <summary>
    /// 装备管理器，每个角色都有
    /// </summary>
    public class EquipmentManager
    {
        public Character chr { get; private set; }

        private ConcurrentDictionary<EquipsType, Equipment> equipDict = new ConcurrentDictionary<EquipsType, Equipment>();

        private InventoryInfo _inventoryInfo;               //网络对象,主要是用作发送给客户端，和保存数据库
        private bool hasChanged;                            //网络对象是否有变化

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
                    _inventoryInfo.Capacity = 0;
                    _inventoryInfo.List.Clear();
                    foreach (var item in equipDict.Values)
                    {
                        _inventoryInfo.List.Add(item.ItemInfo);
                    }
                    hasChanged = false;
                }
                return _inventoryInfo;
            }
        }

        public EquipmentManager(Character chr)
        {
            this.chr = chr;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="bytes"></param>
        public void Init(byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }

            //数据还原 binary => Inventory
            InventoryInfo inv = InventoryInfo.Parser.ParseFrom(bytes);

            //创建物品
            foreach (var iteminfo in inv.List)
            {
                Wear(new Equipment(iteminfo));
            }
        }


        /// <summary>
        /// 穿戴装备
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        public bool Wear(Equipment equipment)
        {
            //穿戴之前，先卸下来
            if (!Unload(equipment.EquipsType)) return false;
            equipDict[equipment.EquipsType] = equipment;
            hasChanged = true;
            return true;
        }

        /// <summary>
        /// 卸下装备    
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Unload(EquipsType type)
        {
            if(equipDict.TryGetValue(type,out var equipment))
            {
                int amount = chr.knapsack.AddItem(equipment);
                //放回背包失败
                if (amount <= 0)
                {
                    return false;
                }
                //放回背包成功,从字典中移除
                equipDict.TryRemove(type, out _);
                hasChanged = true;
                return true;
            }
            return false;
        }

    }
}
