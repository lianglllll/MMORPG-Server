using Proto;
using Summer;
using Summer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Database;
using GameServer.Manager;
using Serilog;
using GameServer.Core;
using GameServer.InventorySystem;
using GameServer.core;
using System.Runtime.ConstrainedExecution;

namespace GameServer.Model
{
    /// <summary>
    /// 角色，一个玩家可以选择不同的角色
    /// </summary>
    public class Character:Actor
    {
        public DbCharacter Data;                    //当前角色对应的数据库对象信息
        public Inventory knapsack;                  //背包 
        public EquipmentManager equipmentManager;   //装备管理器

        public Session session
        {
            get;
            set;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbCharacter"></param>
        public Character(DbCharacter dbCharacter) : base(EntityType.Character,dbCharacter.JobId,dbCharacter.Level,new Vector3Int(dbCharacter.X, dbCharacter.Y, dbCharacter.Z), Vector3Int.zero)
        {

            //将角色信息转换为Character
            this.Id = dbCharacter.Id;
            this.Data = dbCharacter;

            this.Name = dbCharacter.Name;              //覆盖
            if(dbCharacter.Hp <= 0)
            {
                this.Hp = Attr.final.HPMax;
            }
            else
            {
                this.Hp = dbCharacter.Hp;                  //覆盖
            }
            this.Mp = dbCharacter.Mp;                  //覆盖
            this.SpaceId = dbCharacter.SpaceId;        //覆盖

            this.Id = dbCharacter.Id;                  //独有
            this.Exp = dbCharacter.Exp;                //独有
            this.Gold = dbCharacter.Gold;              //独有


            //创建背包
            knapsack = new Inventory(this);
            knapsack.Init(Data.Knapsack);

            //装备栏
            equipmentManager = new EquipmentManager(this);
            equipmentManager.Init(Data.EquipsData);

            //Log.Information("2[角色]：{0} 【背包信息】：[容量]={1},[物品]={2} ", dbCharacter.Name, knapsack.Capacity,knapsack.InventoryInfo);
            //Log.Information("【装备信息】：" + equipmentManager.InventoryInfo.List);
        }

        /// <summary>
        /// 重载运算符=,隐式类型转换
        /// </summary>
        /// <param name="dbCharacter"></param>
        public static implicit operator Character(DbCharacter dbCharacter)
        {
            return new Character(dbCharacter);
        }

        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="slotIndex"></param>
        public bool UseItem(int slotIndex,int count)
        {
            //判断有无这个物品
            var item = knapsack.GetItemBySlotIndex(slotIndex);
            if (item == null) return false;

            //判断类型是否正确
            if (item.GetItemType() != ItemType.Consumable) return false;

            //判断是否可以使用
            if (count > item.Amount) return false;

            item.Amount -= count;
            if(item.Amount <= 0)
            {
                knapsack.removeSlot(slotIndex);
            }

            //产生物品使用效果
            if(item.ItemId == 1001)
            {
                SetHp(Hp + 50);
            }else if(item.ItemId == 1002)
            {
                SetMP(Mp + 50);
            }


            //发送响应结果
            ItemUseResponse resp = new ItemUseResponse();
            resp.Result = true;
            resp.SlotIndex = slotIndex;
            resp.Count = count;
            session.Send(resp);
            return true;
        
        }

        /// <summary>
        /// 设置chr的经验
        /// </summary>
        public void SetExp(long exp)
        {
            if (Exp == exp) return;
            long oldExp = Exp;
            Exp = exp;

            //判断当前经验是否足够升级
            while (DataManager.Instance.levelDefindeDict.TryGetValue(Level, out var define))
            {
                if (Exp >= define.ExpLimit)
                {
                    this.SetLevel(Level + 1);
                    Exp -= define.ExpLimit;
                }
                else
                {
                    break;
                }
            }

            //发包
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Exp,
                OldValue = new() { LongValue = oldExp },
                NewValue = new() { LongValue = Exp }
            };

            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 设置chr的金币
        /// </summary>
        public void SetGold(long gold)
        {
            if (Gold == gold) return;
            long oldgold = Gold;
            Gold = gold;

            //发包
            PropertyUpdate po = new PropertyUpdate()
            {
                EntityId = EntityId,
                Property = PropertyUpdate.Types.Prop.Golds,
                OldValue = new() { LongValue = oldgold },
                NewValue = new() { LongValue = Gold }
            };

            Log.Information("金币=" + Gold);

            currentSpace.fightManager.propertyUpdateQueue.Enqueue(po);
        }

        /// <summary>
        /// 复活
        /// </summary>
        public override void Revive()
        {
            if (!IsDeath) return;
            SetHp(Attr.final.HPMax);
            SetMP(Attr.final.MPMax);
            SetState(UnitState.Free);
            //设置当前角色的位置：找到场景中最近的复活点
            Position = currentSpace.SearchNearestRevivalPoint(this);
            SetEntityState(EntityState.Idle);
            OnAfterRevive();
        }

        public override void SetEntityState(EntityState state)
        {
            //这里我们同步给别人和同步给自己的客户端不使用同一个协议
            this.State = state;
            var resp = new CtlClientSpaceEntitySyncResponse();
            resp.EntitySync = new NEntitySync();
            resp.EntitySync.Entity = EntityData;
            resp.EntitySync.Force = true;
            resp.EntitySync.State = state;
            //发给其他玩家
            currentSpace.UpdateEntity(resp.EntitySync);
            //发给自己
            session.Send(resp);

        }


    }


}
