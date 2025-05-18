using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.core.Model.BaseItem.Sub;
using GameServer.Core.Model;
using GameServer.InventorySystem;
using GameServer.Net;
using HS.Protobuf.Backpack;
using HS.Protobuf.Chat;
using HS.Protobuf.GameTask;
using HS.Protobuf.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Hanle
{
    public class GameItemHandler : Singleton<GameItemHandler>
    {
        public override void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetItemInventoryDataRequest>((int)BackpackProtocol.GetItemInventoryDataReq);
            ProtoHelper.Instance.Register<GetItemInventoryDataResponse>((int)BackpackProtocol.GetItemInventoryDataResp);
            ProtoHelper.Instance.Register<ChangeItemPositionRequest>((int)BackpackProtocol.ChangeItemPositionReq);
            ProtoHelper.Instance.Register<ChangeItemPositionResponse>((int)BackpackProtocol.ChangeItemPositionResp);
            ProtoHelper.Instance.Register<UseItemRequest>((int)BackpackProtocol.UseItemReq);
            ProtoHelper.Instance.Register<UseItemResponse>((int)BackpackProtocol.UseItemResp);
            ProtoHelper.Instance.Register<DiscardItemRequest>((int)BackpackProtocol.DiscardItemReq);
            ProtoHelper.Instance.Register<DiscardItemResponse>((int)BackpackProtocol.DiscardItemResp);
            ProtoHelper.Instance.Register<DiscardGameItemToSceneRequest>((int)SceneProtocl.DiscardGameItemToSceneReq);
            ProtoHelper.Instance.Register<DiscardGameItemToSceneResponse>((int)SceneProtocl.DiscardGameItemToSceneResp);
            ProtoHelper.Instance.Register<WearEquipRequest>((int)BackpackProtocol.WearEquipReq);
            ProtoHelper.Instance.Register<WearEquipResponse>((int)BackpackProtocol.WearEquipResp);
            ProtoHelper.Instance.Register<UnloadEquipRequest>((int)BackpackProtocol.UnloadEquipReq);
            ProtoHelper.Instance.Register<UnloadEquipResponse>((int)BackpackProtocol.UnloadEquipResp);
            ProtoHelper.Instance.Register<ChangeEquipmentToSceneRequest>((int)SceneProtocl.ChangeEquipmentToSceneReq);
            ProtoHelper.Instance.Register<ChangeEquipmentToSceneResponse>((int)SceneProtocl.ChangeEquipmentToSceneResp);
            ProtoHelper.Instance.Register<PickUpSceneItemToGameRequest>((int)BackpackProtocol.PickUpGameItemToGameReq);
            ProtoHelper.Instance.Register<PickUpSceneItemToGameResponse>((int)BackpackProtocol.PickUpGameItemToGameResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetItemInventoryDataRequest>(HandleGetItemInventoryDataRequest);
            MessageRouter.Instance.Subscribe<ChangeItemPositionRequest>(HandleChangeItemPositionRequest);
            MessageRouter.Instance.Subscribe<UseItemRequest>(HandleUseItemRequest);
            MessageRouter.Instance.Subscribe<DiscardItemRequest>(HandleDiscardItemRequest);
            MessageRouter.Instance.Subscribe<WearEquipRequest>(HandleWearEquipRequest);
            MessageRouter.Instance.Subscribe<UnloadEquipRequest>(HandleUnloadEquipRequest);
            MessageRouter.Instance.Subscribe<PickUpSceneItemToGameRequest>(HandlePickUpSceneItemToGameRequest);
        }
        private void HandleGetItemInventoryDataRequest(Connection conn, GetItemInventoryDataRequest message)
        {
            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }

            var resp = new GetItemInventoryDataResponse();
            resp.SessionId = chr.SessionId;
            if(message.Type == ItemInventoryType.Backpack)
            {
                resp.Node = chr.BackPackManager.NetItemInventoryDataNode;
            }
            else if(message.Type == ItemInventoryType.Equipments)
            {
                resp.Node = chr.EquipmentManager.NetItemInventoryDataNode;
            }else if(message.Type == ItemInventoryType.Warehouse)
            {

            }
            conn.Send(resp);
        End:
            return;
        }
        private void HandleChangeItemPositionRequest(Connection conn, ChangeItemPositionRequest message)
        {
            var resp = new ChangeItemPositionResponse();
            resp.SessionId = message.SessionId;
            resp.Seq = message.Seq;

            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                resp.ResultCode = 11;
                resp.ResultMsg = "chr为空";
                goto End;
            }

            if(message.OriginIndex == message.TargetIndex)
            {
                resp.ResultCode = 12;
                resp.ResultMsg = "起始和目标位置相同";
                goto End;
            }

            InventoryManager originInventory = null;
            if(message.OriginInventory == ItemInventoryType.Backpack)
            {
                originInventory = chr.BackPackManager;
            }else if(message.OriginInventory == ItemInventoryType.Warehouse)
            {

            }
            var originItem = originInventory.GetItemByGridIdx(message.OriginIndex);
            if(originItem == null)
            {
                resp.ResultCode = 13;
                resp.ResultMsg = "originItem 不能为空";
                goto End;
            }
            originInventory.RemoveGrid(message.OriginIndex);

            InventoryManager targetInventory = null;
            if (message.TargetInventory == ItemInventoryType.Backpack)
            {
                targetInventory = chr.BackPackManager;
            }
            else if (message.TargetInventory == ItemInventoryType.Warehouse)
            {

            }
            var targetItem = targetInventory.GetItemByGridIdx(message.TargetIndex);
            if(targetItem == null)
            {
                targetInventory.AddGameItemToTargetGrid(originItem, message.TargetIndex);
            }
            else
            {
                targetInventory.RemoveGrid(message.TargetIndex);
                targetInventory.AddGameItemToTargetGrid(originItem, message.TargetIndex);
                originInventory.AddGameItemToTargetGrid(targetItem, message.OriginIndex);
            }
            resp.ResultCode = 0; 
        End:
            conn.Send(resp);
            return;
        }
        private void HandleUseItemRequest(Connection conn, UseItemRequest message)
        {
            var resp = new UseItemResponse();
            resp.SessionId = message.SessionId; 

            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }

            chr.BackPackManager.UseItem(message.GridIndex, message.Count);
            resp.ResultCode = 0;
            conn.Send(resp);
        End:
            return;
        }
        private void HandleDiscardItemRequest(Connection conn, DiscardItemRequest message)
        {
            var resp = new ChangeItemPositionResponse();
            resp.SessionId = message.SessionId;
            resp.Seq = message.Seq;

            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                resp.ResultCode = 11;
                resp.ResultMsg = "chr为空";
                goto End;
            }

            if(message.Type == ItemInventoryType.Backpack)
            {
                chr.BackPackManager.Discard(message.GridIndex, message.Count);
            }
            else if(message.Type == ItemInventoryType.Warehouse)
            {

            }

            resp.ResultCode = 0;
        End:
            conn.Send(resp);
            return;
        }
        private void HandleWearEquipRequest(Connection conn, WearEquipRequest message)
        {
            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }

            var item = chr.BackPackManager.GetItemByGridIdx(message.GridIndex);
            if(item == null || !(item is GameEquipment gameEquipment))
            {
                goto End;
            }
            chr.EquipmentManager.WearEquipment(gameEquipment, message.EquipSlotType);
        End:
            return;
        }
        private void HandleUnloadEquipRequest(Connection conn, UnloadEquipRequest message)
        {
            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }
            chr.EquipmentManager.UnloadEquipment(message.Type, true);
        End:
            return;
        }
        private void HandlePickUpSceneItemToGameRequest(Connection conn, PickUpSceneItemToGameRequest message)
        {
            var resp = new PickUpSceneItemToGameResponse();

            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End2;
            }
            resp.EntityId = chr.EntityId;

            var itemDateNode = message.ItemDataNode;
            if(!chr.BackPackManager.IsCanAddToInventory(itemDateNode.ItemId, itemDateNode.Amount))
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "背包无法一次性装下当前物品";
                goto End1;
            }

            chr.BackPackManager.AddGameItem(itemDateNode.ItemId, itemDateNode.Amount);

        End1:
            conn.Send(resp);
        End2:
            return;
        }
    }
}
