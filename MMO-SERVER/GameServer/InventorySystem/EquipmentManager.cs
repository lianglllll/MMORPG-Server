using GameServer.Model;
using GameServer.Service;
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
        /// 获取装备栏上的装备
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Equipment GetEquipment(EquipsType type)
        {
            return equipDict.GetValueOrDefault(type, null);
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
                Wear(new Equipment(iteminfo), false);
            }

            //更新chr装备信息
            chr.info.EquipList.Clear();
            chr.info.EquipList.AddRange(InventoryInfo.List);
        }

        /// <summary>
        /// 穿戴装备
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        public bool Wear(Equipment equipment, bool isBroadcast)
        {
            //穿戴之前，先卸下来身上的装备
            Unload(equipment.EquipsType, isBroadcast);

            //穿到身上
            equipDict[equipment.EquipsType] = equipment;
            int slot = equipment.Position;
            equipment.Position = -1;

            //移除背包中的
            chr.knapsack.removeSlot(slot);

            //增加属性
            chr.Attr.equip.Merge(equipment.attrubuteData);
            chr.Attr.Reload();

            hasChanged = true;

            //更新chr的网络actor的info并且广播给其他客户端
            UpdateCharacterInfoEquips(isBroadcast);

            return true;
        }

        /// <summary>
        /// 卸下装备    
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Unload(EquipsType type, bool isBroadcast)
        {
            //没有这个装备
            if (!equipDict.TryGetValue(type, out var equipment)) return false;
            
            int amount = chr.knapsack.AddItem(equipment);
            //放回背包失败
            if (amount > 0)
            {
                //通知客户端

            }
            else
            {
                return false;
            }

            //放回背包成功,从字典中移除
            equipDict.TryRemove(type, out _);

            //chr减少属性
            chr.Attr.equip.Sub(equipment.attrubuteData);
            chr.Attr.Reload();

            hasChanged = true;

            //更新chr的网络actor的info并且广播给其他客户端
            UpdateCharacterInfoEquips(isBroadcast);

            return true;

        }

        /// <summary>
        /// 装备发生变更时，通知客户端
        /// </summary>
        public void UpdateCharacterInfoEquips(bool isBroadcast)
        {
            //更新chr装备信息
            chr.info.EquipList.Clear();
            chr.info.EquipList.AddRange(InventoryInfo.List);

            //广播
            if (isBroadcast)
            {
                //Log.Information("广播装备信息");
                ItemService.Instance._EquipsUpdateResponse(chr);
            }

        }
    }
}
