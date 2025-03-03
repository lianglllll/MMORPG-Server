using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Scene;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene;


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
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ActorChangeModeRequest>(_HandleActorChangeModeRequest);
            MessageRouter.Instance.Subscribe<ActorChangeStateRequest>(_HandleActorChangeStateRequest);
            MessageRouter.Instance.Subscribe<ActorChangeTransformDataRequest>(_HandleActorChangeTransformDataRequest);

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
    }
}
