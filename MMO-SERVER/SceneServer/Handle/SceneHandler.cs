using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Backpack;
using HS.Protobuf.Scene;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Model.Item;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;
using SceneServer.Net;
using Serilog;

namespace SceneServer.Handle
{
    public class SceneHandler : Singleton<SceneHandler>
    {
        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<OtherEntityEnterSceneResponse>((int)SceneProtocl.OtherEntityEnterSceneResp);
            ProtoHelper.Instance.Register<OtherEntityLeaveSceneResponse>((int)SceneProtocl.OtherEntityLeaveSceneResp);

            ProtoHelper.Instance.Register<ActorChangeModeRequest>((int)SceneProtocl.ActorChangeModeReq);
            ProtoHelper.Instance.Register<ActorChangeModeResponse>((int)SceneProtocl.ActorChangeModeResp);
            ProtoHelper.Instance.Register<ActorChangeStateRequest>((int)SceneProtocl.ActorChangeStateReq);
            ProtoHelper.Instance.Register<ActorChangeStateResponse>((int)SceneProtocl.ActorChangeStateResp);
            ProtoHelper.Instance.Register<ActorChangeTransformDataRequest>((int)SceneProtocl.ActorChangeTransformDataReq);
            ProtoHelper.Instance.Register<ActorChangeTransformDataResponse>((int)SceneProtocl.ActorChangeTransformDataResp);

            ProtoHelper.Instance.Register<PickUpSceneItemRequest>((int)SceneProtocl.PickupSceneItemReq);
            ProtoHelper.Instance.Register<PickupSceneItemResponse>((int)SceneProtocl.PickupSceneItemResp);
            ProtoHelper.Instance.Register<PickUpSceneItemToGameRequest>((int)BackpackProtocol.PickUpGameItemToGameReq);
            ProtoHelper.Instance.Register<PickUpSceneItemToGameResponse>((int)BackpackProtocol.PickUpGameItemToGameResp);

            ProtoHelper.Instance.Register<DiscardGameItemToSceneRequest>((int)SceneProtocl.DiscardGameItemToSceneReq);
            ProtoHelper.Instance.Register<DiscardGameItemToSceneResponse>((int)SceneProtocl.DiscardGameItemToSceneResp);
            ProtoHelper.Instance.Register<ChangeEquipmentToSceneRequest>((int)SceneProtocl.ChangeEquipmentToSceneReq);
            ProtoHelper.Instance.Register<ChangeEquipmentToSceneResponse>((int)SceneProtocl.ChangeEquipmentToSceneResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ActorChangeModeRequest>(_HandleActorChangeModeRequest);
            MessageRouter.Instance.Subscribe<ActorChangeStateRequest>(_HandleActorChangeStateRequest);
            MessageRouter.Instance.Subscribe<ActorChangeTransformDataRequest>(_HandleActorChangeTransformDataRequest);

            MessageRouter.Instance.Subscribe<PickUpSceneItemRequest>(_HandlePickUpSceneItemRequest);

            MessageRouter.Instance.Subscribe<DiscardGameItemToSceneRequest>(_HandleDiscardGameItemToSceneRequest);
            MessageRouter.Instance.Subscribe<ChangeEquipmentToSceneRequest>(_HandleChangeEquipmentToSceneRequest);

            return true;
        }

        private void _HandleActorChangeModeRequest(Connection conn, ActorChangeModeRequest message)
        {
            // 这里只能是player发信息过来的
            var actor = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneActor;
            if (actor == null)
            {
                goto End;
            }

            // 不接受死亡角色的状态切换(我们有专门的复活协议处理)
            if (actor.IsDeath)
            {
                goto End;
            }
            SceneManager.Instance.ActorChangeMode(actor, message);
        End:
            return;
        }
        private void _HandleActorChangeStateRequest(Connection conn, ActorChangeStateRequest message)
        {
            // 这里只能是player发信息过来的
            var actor = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneActor;
            if(actor == null)
            {
                goto End;
            }
            // 不接受死亡角色的状态切换(我们有专门的复活协议处理)
            if (actor.IsDeath)
            {
                goto End;
            }
            SceneManager.Instance.ActorChangeState(actor, message);
        End:
            return;
        }
        private void _HandleActorChangeTransformDataRequest(Connection conn, ActorChangeTransformDataRequest message)
        {
            // 这里只能是player发信息过来的
            var actor = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneActor;
            if (actor == null)
            {
                goto End;
            }
            // 不接受死亡角色的状态切换(我们有专门的复活协议处理)
            if (actor.IsDeath)
            {
                goto End;
            }
            SceneManager.Instance.ActorChangeTransformData(actor, message);
        End:
            return;
        }
        private void _HandlePickUpSceneItemRequest(Connection conn, PickUpSceneItemRequest message)
        {
            // 这里只能是player发信息过来的
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End;
            }
            var sceneItem = SceneManager.Instance.SceneItemManager.RemoveItem(message.ItemEntityId);
            if(sceneItem == null)
            {
                goto End;
            }

            // 将物品放入背包
            var req = new PickUpSceneItemToGameRequest();
            req.CId = chr.Cid;
            req.ItemDataNode = sceneItem.NetItemNode.NetItemDataNode;
            ServersMgr.Instance.SendToGame(req);

        End:
            return;
        }
        private void _HandleDiscardGameItemToSceneRequest(Connection conn, DiscardGameItemToSceneRequest message)
        {
            // 这里只能是player发信息过来的
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End;
            }
            SceneManager.Instance.SceneItemManager.CreateSceneItem(message.ItemDataNode, chr.Position, chr.Rotation, Vector3.One);
        End:
            return;
        }
        private void _HandleChangeEquipmentToSceneRequest(Connection conn, ChangeEquipmentToSceneRequest message)
        {
            // 这里只能是player发信息过来的
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End;
            }

            if(message.OperationType == ChangeEquipmentOperationType.Unload)
            {
                chr.AttributeManager.UnloadEquip(message.EquipNode);
            }else if(message.OperationType == ChangeEquipmentOperationType.Wear)
            {
                chr.AttributeManager.AddEquip(message.EquipNode);
            }

        End:
            return;
        }
    }
}
