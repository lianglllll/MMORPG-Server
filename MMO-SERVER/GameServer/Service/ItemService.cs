using GameServer.core;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using Proto;
using Serilog;
using Summer;
using Summer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Service
{

    public class ItemService:Singleton<ItemService>
    {
        public void start()
        {
            MessageRouter.Instance.Subscribe<InventoryInfoRequest>(_InventoryInfoRequest);
            MessageRouter.Instance.Subscribe<ItemPlacementRequest>(_ItemPlacementRequest);
            MessageRouter.Instance.Subscribe<ItemUseRequest>(_ItemUseRequest);
        }


        /// <summary>
        /// 处理玩家获取inventory信息请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _InventoryInfoRequest(Connection conn, InventoryInfoRequest message)
        {
            //安全校验
            Entity entity = EntityManager.Instance.GetEntity(message.EntityId);
            if (entity == null) return;
            if (!((EntityManager.Instance.GetEntity(message.EntityId)) is Character chr)) return;

            //响应给客户端
            InventoryInfoResponse resp = new InventoryInfoResponse();
            resp.EntityId = chr.EntityId;
            if (message.QueryKnapsack)
            {
                resp.KnapsackInfo = chr.knapsack.InventoryInfo;
            }
            if (message.QueryWarehouse)
            {

            }
            if (message.QueryEquipment)
            {

            }
            conn.Send(resp);
        }

        /// <summary>
        /// 处理玩家使用物品请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ItemUseRequest(Connection conn, ItemUseRequest message)
        {
            bool result = ItemManager.Instance.ItemUse(message);
            if(result == false)
            {
                ItemUseResponse resp = new ItemUseResponse();
                resp.Result = false;
                conn.Send(resp);
            }
        }

        /// <summary>
        /// 处理玩家item的放置请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ItemPlacementRequest(Connection conn, ItemPlacementRequest message)
        {
            //安全校验
            Entity entity = EntityManager.Instance.GetEntity(message.EntityId);
            if (entity == null) return;
            if (!(entity is Character)) return;

            //创建响应包
            ItemPlacementResponse resp = new ItemPlacementResponse();
            resp.ActionId = message.ActionId;


            //分情况处理
            bool result = false;
            if(message.OriginInventoryTpey == InventoryType.Knapsack)
            {
                result = ProcessKnapsackTo(message, entity as Character);
            }
            else if(message.OriginInventoryTpey == InventoryType.Warehouse)
            {

            }else if (message.OriginInventoryTpey == InventoryType.EquipmentColumn)
            {

            }else if(message.OriginInventoryTpey == InventoryType.CurrentScene)
            {
                int amount = 0;
                result = ProcessCurrentSceneTo(message, entity as Character,ref amount);
                resp.AuxiliarySpace = amount;               //成功添加物品的数量
            }

            resp.Result = result;
            conn.Send(resp);
        }

        /// <summary>
        /// 处理Knapsack到某个inventory的情况
        /// </summary>
        /// <param name="message"></param>
        public bool ProcessKnapsackTo(ItemPlacementRequest message,Character chr)
        {
            if (message.TargetInventoryTpey == InventoryType.Knapsack)
            {
                return chr.knapsack.Exchange(message.OriginIndex, message.TargetIndex);
            }
            else if(message.TargetInventoryTpey == InventoryType.Warehouse)
            {

            }else if(message.TargetInventoryTpey == InventoryType.EquipmentColumn)
            {

            }else if(message.TargetInventoryTpey == InventoryType.CurrentScene)
            {
                //其实是丢弃，这里的result就是丢弃的数量
                Log.Information("丢弃物品{0}", message);
                int result = chr.knapsack.Discard(message.OriginIndex, message.TargetIndex);
                if(result > 0)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// 处理CurrentScene到某个inventory的情况
        /// </summary>
        /// <param name="message"></param>
        /// <param name="character"></param>
        /// <returns></returns>
        private bool ProcessCurrentSceneTo(ItemPlacementRequest message, Character character, ref int amount)
        {
            if (message.TargetInventoryTpey == InventoryType.Knapsack)
            {
                //current->knapsack其实就是拾起操作
                amount =  PickupItemAction(message,character);
                if (amount > 0)
                {
                    return true;
                }
                return false;
            }
            else if (message.TargetInventoryTpey == InventoryType.Warehouse)
            {

            }
            else if (message.TargetInventoryTpey == InventoryType.EquipmentColumn)
            {

            }
            else if (message.TargetInventoryTpey == InventoryType.CurrentScene)
            {

            }
            return false;
        }

        //等弄一个itemmanager来统一放这些个操作，service只处理请求的，负责转发
        /// <summary>
        /// 拾取当前物品操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private int PickupItemAction(ItemPlacementRequest message,Character chr)
        {
            //这里通过entity来寻找
            int itemEntityId = message.OriginIndex;

            //获取一个符合条件的item，如果没有就忽略这次请求
            Entity entity = GameTools.GetEntity(itemEntityId);

            if (entity == null) return 0;
            if (!(entity is ItemEntity itemEntity)) return 0;

            //添加物品到背包
             var alreadyAddedAmount = chr.knapsack.AddItem(itemEntity.Item.ItemId, itemEntity.Item.Amount);

            //判别是否装得下
            if (alreadyAddedAmount == itemEntity.Item.Amount)
            {
                //如果背包能装下全部，则通知场景中这个物品已经消失
                chr.currentSpace.ItemLeave(itemEntity);
            }
            else
            {
                //更新场景中的itementity数据,amount
                itemEntity.Item.Amount -= alreadyAddedAmount;
                chr.currentSpace.SyncItemEntity(itemEntity);
            }

            Log.Information("玩家拾起物品chr[{0}],背包[{1}]", chr.EntityId, chr.knapsack.InventoryInfo);
            return alreadyAddedAmount;
        }

    }
}
