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
            ProtoHelper.Instance.Register<ActorChangeModeRequest>((int)SceneProtocl.ActorChangeModeReq);
            ProtoHelper.Instance.Register<ActorChangeModeResponse>((int)SceneProtocl.ActorChangeModeResp);
            ProtoHelper.Instance.Register<ActorChangeStateRequest>((int)SceneProtocl.ActorChangeStateReq);
            ProtoHelper.Instance.Register<ActorChangeStateResponse>((int)SceneProtocl.ActorChangeStateResp);
            ProtoHelper.Instance.Register<ActorChangeMotionDataRequest>((int)SceneProtocl.ActorChangeMotionDataReq);
            ProtoHelper.Instance.Register<ActorChangeMotionDataResponse>((int)SceneProtocl.ActorChangeMotionDataResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ActorChangeModeRequest>(_HandleActorChangeModeRequest);
            MessageRouter.Instance.Subscribe<ActorChangeStateRequest>(_HandleActorChangeStateRequest);
            MessageRouter.Instance.Subscribe<ActorChangeMotionDataRequest>(_HandleActorChangeMotionDataRequest);

            return true;
        }

        private void _HandleActorChangeModeRequest(Connection conn, ActorChangeModeRequest message)
        {
            throw new NotImplementedException();
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
        private void _HandleActorChangeMotionDataRequest(Connection conn, ActorChangeMotionDataRequest message)
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
            SceneManager.Instance.ActorChangeMotionData(actor, message);
        End:
            return;
        }
    }
}
