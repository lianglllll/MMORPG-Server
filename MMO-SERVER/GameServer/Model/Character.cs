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

namespace GameServer.Model
{
    /// <summary>
    /// 角色，一个玩家可以选择不同的角色
    /// </summary>
    public class Character:Actor
    {

        public Connection conn;         //当前角色的客户端
        public DbCharacter Data;        //当前角色对应的数据库对象信息
        public Inventory knapsack;      //背包 

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbCharacter"></param>
        public Character(DbCharacter dbCharacter) : base(EntityType.Character,dbCharacter.JobId,dbCharacter.Level,new Vector3Int(dbCharacter.X, dbCharacter.Y, dbCharacter.Z), Vector3Int.zero)
        {

            //将角色信息转换为Character
            this.Id = dbCharacter.Id;
            this.Name = dbCharacter.Name;
            this.Data = dbCharacter;

            this.info.Name = dbCharacter.Name;              //覆盖
            this.info.Hp = dbCharacter.Hp;                  //覆盖
            this.info.Mp = dbCharacter.Mp;                  //覆盖

            this.info.Id = dbCharacter.Id;                  //独有
            this.info.Exp = dbCharacter.Exp;                //独有
            this.info.SpaceId = dbCharacter.SpaceId;        //独有
            this.info.Gold = dbCharacter.Gold;              //独有

            //this.info.Entity;      使用时需要自动调用entitydata进行赋值
            //this.info.Entity.Id;
            //this.EntityId;        entityid 等待entitymanager分配id

            //创建背包
            knapsack = new Inventory(this);
            knapsack.Init(Data.Knapsack);

            Log.Information("[角色]：{0} 【背包信息】：[容量]={1},[物品]={2} ", dbCharacter.Name, knapsack.Capacity,knapsack.InventoryInfo);
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
            conn.Send(resp);
            return true;
        
        }


    }


}
