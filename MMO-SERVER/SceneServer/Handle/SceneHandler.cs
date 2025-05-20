using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Backpack;
using HS.Protobuf.GameTask;
using HS.Protobuf.Scene;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Model.Item;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;
using SceneServer.Net;
using Serilog;
using System.Collections.Concurrent;

namespace SceneServer.Handle
{
    public class SceneHandler : Singleton<SceneHandler>
    {
        public override void Init()
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

            ProtoHelper.Instance.Register<RegisterTaskConditionToSceneRequest>((int)SceneProtocl.RegisterTaskConditionToSceneReq);
            ProtoHelper.Instance.Register<RegisterTaskConditionToSceneresponse>((int)SceneProtocl.RegisterTaskConditionToSceneResp);
            ProtoHelper.Instance.Register<UnRegisterTaskConditionToSceneRequest>((int)SceneProtocl.UnRegisterTaskConditionToSceneReq);
            ProtoHelper.Instance.Register<UnRegisterTaskConditionToSceneResponse>((int)SceneProtocl.UnRegisterTaskConditionToSceneResp);
            ProtoHelper.Instance.Register<SecneTriggerTaskConditionResponse>((int)GameTaskProtocol.SceneTriggerTaskConditionResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ActorChangeModeRequest>(_HandleActorChangeModeRequest);
            MessageRouter.Instance.Subscribe<ActorChangeStateRequest>(_HandleActorChangeStateRequest);
            MessageRouter.Instance.Subscribe<ActorChangeTransformDataRequest>(_HandleActorChangeTransformDataRequest);

            MessageRouter.Instance.Subscribe<PickUpSceneItemRequest>(_HandlePickUpSceneItemRequest);
            MessageRouter.Instance.Subscribe<PickUpSceneItemToGameResponse>(_HandlePickUpSceneItemToGameResponse);

            MessageRouter.Instance.Subscribe<DiscardGameItemToSceneRequest>(_HandleDiscardGameItemToSceneRequest);
            MessageRouter.Instance.Subscribe<ChangeEquipmentToSceneRequest>(_HandleChangeEquipmentToSceneRequest);

            MessageRouter.Instance.Subscribe<RegisterTaskConditionToSceneRequest>(_HandleRegisterTaskConditionToSceneRequest);
            MessageRouter.Instance.Subscribe<UnRegisterTaskConditionToSceneRequest>(_HandleUnRegisterTaskConditionToSceneRequest);

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

        // todo, 这里实际上是有问题的，应该先发给game判断能否装下这么多物品，然后在进行删除。
        // 所以还需要实现一套用于询问game能否装入某某物品的函数。
        private void _HandlePickUpSceneItemRequest(Connection conn, PickUpSceneItemRequest message)
        {
            var resp = new PickupSceneItemResponse();

            // 这里只能是player发信息过来的
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End2;
            }
            resp.SessionId = chr.SessionId;

            var sceneItem = SceneManager.Instance.SceneItemManager.RemoveItem(message.ItemEntityId);
            if(sceneItem == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "该物品不存在于场景中";
                goto End1;
            }

            // 将物品放入背包
            var req = new PickUpSceneItemToGameRequest();
            req.CId = chr.Cid;
            req.ItemDataNode = sceneItem.NetItemNode.NetItemDataNode;
            ServersMgr.Instance.SendToGame(req);
            goto End2;

        End1:
            conn.Send(resp);
        End2:
            return;
        }
        private void _HandlePickUpSceneItemToGameResponse(Connection conn, PickUpSceneItemToGameResponse message)
        {
            var resp = new PickupSceneItemResponse();
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End;
            }

            resp.SessionId = chr.SessionId;
            resp.ResultCode = message.ResultCode;
            resp.ResultMsg= message.ResultMsg;
            if(resp.ResultCode == 0)
            {
                resp.ItemId = message.ItemId;
                resp.Count = message.Count;
            }
            chr.Send(resp);

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

        private void _HandleRegisterTaskConditionToSceneRequest(Connection conn, RegisterTaskConditionToSceneRequest message)
        {
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End;
            }
            chr.GameTaskConditionChecker.RegirsterTaskCondition(message.Conds);
        End:
            return;
        }
        private void _HandleUnRegisterTaskConditionToSceneRequest(Connection conn, UnRegisterTaskConditionToSceneRequest message)
        {
            var chr = SceneEntityManager.Instance.GetSceneEntityById(message.EntityId) as SceneCharacter;
            if (chr == null)
            {
                goto End;
            }
            chr.GameTaskConditionChecker.UnRegirsterTaskCondition(message.TaskId, message.CondTypes);
        End:
            return;
        }
    }
}
