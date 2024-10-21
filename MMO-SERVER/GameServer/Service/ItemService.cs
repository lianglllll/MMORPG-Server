using GameServer.core;
using GameServer.Core;
using GameServer.Database;
using GameServer.InventorySystem;
using GameServer.Manager;
using GameServer.Model;
using Proto;
using Serilog;
using GameServer;
using GameServer.Network;
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
            MessageRouter.Instance.Subscribe<ItemDiscardRequest>(_ItemDiscardRequest);
            MessageRouter.Instance.Subscribe<ItemPickUpRequest>(_ItemPickUpRequest);
            MessageRouter.Instance.Subscribe<WearEquipmentRequest>(_WearEquipmentRequest);
            MessageRouter.Instance.Subscribe<UnloadEquipmentRequest>(_UnloadEquipmentRequest);
        }

        /// <summary>
        /// 处理玩家获取inventory信息请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _InventoryInfoRequest(Connection conn, InventoryInfoRequest message)
        {
            //安全校验
            Entity entity = EntityManager.Instance.GetEntityById(message.EntityId);
            if (entity == null) return;
            if (!((EntityManager.Instance.GetEntityById(message.EntityId)) is Character chr)) return;

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
                resp.EquipmentInfo = chr.equipmentManager.InventoryInfo;
            }
            conn.Send(resp);
        }

        /// <summary>
        /// 处理玩家item的放置请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ItemPlacementRequest(Connection conn, ItemPlacementRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null) return;
            var chr = session.character;
            if (chr == null) return;
            chr.knapsack.Exchange(message.OriginIndex, message.TargetIndex);

            _KnapsacUpdateResponse(conn.Get<Session>().character);
        }

        /// <summary>
        /// 处理玩家使用物品请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ItemUseRequest(Connection conn, ItemUseRequest message)
        {
            bool result = ItemManager.Instance.ItemUse(message);
            if (result == false)
            {
                ItemUseResponse resp = new ItemUseResponse();
                resp.Result = false;
                conn.Send(resp);
            }
            else
            {
                _KnapsacUpdateResponse(conn.Get<Session>().character);
            }

        }

        /// <summary>
        /// 处理玩家物品丢弃请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ItemDiscardRequest(Connection sender, ItemDiscardRequest message)
        {
            var session = sender.Get<Session>();
            if (session == null) return;
            var chr = session.character;
            if (chr == null) return;
            var item = chr.knapsack.GetItemBySlotIndex(message.SlotIndex);
            //丢弃
            int discardAmount = chr.knapsack.Discard(message.SlotIndex,message.Number);

            //刷ui
            _KnapsacUpdateResponse(chr);

            var res = new ItemDiscardResponse();
            if (discardAmount > 0)
            {
                res.Result = Result.Success;
                res.ItemId = item.ItemId;
                res.Amout = discardAmount;
            }
            else
            {
                res.Result = Result.Fault;
            }
            sender.Send(res);
        }

        /// <summary>
        /// 处理玩家物品拾取请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _ItemPickUpRequest(Connection sender, ItemPickUpRequest message)
        {
            var session = sender.Get<Session>();
            if (session == null) return;
            var chr = session.character;
            if (chr == null) return;

            chr.currentSpace.actionQueue.Enqueue(() => {

                //获取符合条件的item，如果没有就忽略这次请求
                EItem eItem = chr.currentSpace.itemManager.GetEItemByEntityId(message.EntityId);
                if (eItem == null) return;

                //添加物品到背包
                int alreadyAddedAmount = 0;
                if (eItem.Item.GetItemType() == ItemType.Equipment)
                {
                    alreadyAddedAmount = chr.knapsack.AddItem(eItem.Item);
                }
                else
                {
                    alreadyAddedAmount = chr.knapsack.AddItem(eItem.Item.ItemId, eItem.Item.Amount);
                }

                //判别是否装得下
                if (alreadyAddedAmount == eItem.Item.Amount)
                {
                    //如果背包能装下全部，则通知场景中这个物品已经消失
                    chr.currentSpace.itemManager.RemoveItem(eItem);
                }
                else if (alreadyAddedAmount < eItem.Item.Amount && alreadyAddedAmount != 0)
                {
                    //更新场景中的itementity数据,amount
                    eItem.Item.Amount -= alreadyAddedAmount;
                    NetItemEntitySync resp = new NetItemEntitySync();
                    resp.NetItemEntity = eItem.NetItemEntity;
                    chr.currentSpace.AOIBroadcast(eItem,resp);
                }
                else
                {
                    //添加失败
                }


                //响应客户端
                var res = new ItemPickupResponse();
                if (alreadyAddedAmount > 0)
                {
                    res.Result = Result.Success;
                    res.ItemId = eItem.Item.ItemId;
                    res.Amout = alreadyAddedAmount;
                    //更新ui
                    _KnapsacUpdateResponse(chr);
                }
                else
                {
                    res.Result = Result.Fault;
                }
                sender.Send(res);

            });

        }

        /// <summary>
        /// 装备栏中的装备卸载请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _UnloadEquipmentRequest(Connection sender, UnloadEquipmentRequest message)
        {
            var session = sender.Get<Session>();
            if (session == null) return;
            var chr = session.character;
            if (chr == null) return;

            var equipsType = message.Type;
            var item = chr.equipmentManager.GetEquipment(equipsType);
            if (item == null) return;

            chr.equipmentManager.Unload(equipsType, true);

            _KnapsacUpdateResponse(chr);

        }

        /// <summary>
        /// 背包中的装备穿戴请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _WearEquipmentRequest(Connection sender, WearEquipmentRequest message)
        {
            var session = sender.Get<Session>();
            if (session == null) return;
            var chr = session.character;
            if (chr == null) return;

            var slotIndex = message.SlotIndex;
            var item = chr.knapsack.GetItemBySlotIndex(slotIndex);
            if (item == null || !(item is Equipment equipment)) return;

            chr.equipmentManager.Wear(equipment, true);

            _KnapsacUpdateResponse(chr);
        }

        /// <summary>
        /// 背包更新响应
        /// </summary>
        /// <param name="chr"></param>
        public void _KnapsacUpdateResponse(Character chr)
        {
            //响应给客户端
            InventoryInfoResponse resp = new InventoryInfoResponse();
            resp.EntityId = chr.EntityId;
            resp.KnapsackInfo = chr.knapsack.InventoryInfo;

            chr.session.Send(resp);
        }

        /// <summary>
        /// 装备更新响应
        /// </summary>
        public void _EquipsUpdateResponse(Character chr)
        {
            var resp = new EquipsUpdateResponse();
            resp.EntityId = chr.EntityId;
            resp.EquipsList.AddRange(chr.Info.EquipList);

            //广播
            chr.currentSpace.AOIBroadcast(chr, resp, true);
        }
    }
}
