using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using HS.Protobuf.Game;
using HS.Protobuf.GameGate;
using HS.Protobuf.Scene;

namespace GameGateServer.Handle
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
            ProtoHelper.Instance.Register<ActorChangeMotionDataRequest>((int)SceneProtocl.ActorChangeMotionDataReq);
            ProtoHelper.Instance.Register<ActorChangeMotionDataResponse>((int)SceneProtocl.ActorChangeMotionDataResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<OtherEntityEnterSceneResponse>(_HandleOtherEntityEnterSceneResponse);
            MessageRouter.Instance.Subscribe<OtherEntityLeaveSceneResponse>(_HandleOtherEntityLeaveSceneResponse);
            MessageRouter.Instance.Subscribe<ActorChangeModeRequest>(_HandleActorChangeModeRequest);
            MessageRouter.Instance.Subscribe<ActorChangeModeResponse>(_HandleActorChangeModeResponse);
            MessageRouter.Instance.Subscribe<ActorChangeStateRequest>(_HandleActorChangeStateRequest);
            MessageRouter.Instance.Subscribe<ActorChangeStateResponse>(_HandleActorChangeStateResponse);
            MessageRouter.Instance.Subscribe<ActorChangeMotionDataRequest>(_HandleActorChangeMotionDataRequest);
            MessageRouter.Instance.Subscribe<ActorChangeMotionDataResponse>(_HandleActorChangeMotionDataResponse);

            return true;
        }

        private void _HandleOtherEntityEnterSceneResponse(Connection conn, OtherEntityEnterSceneResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }
        private void _HandleOtherEntityLeaveSceneResponse(Connection conn, OtherEntityLeaveSceneResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void _HandleActorChangeModeRequest(Connection conn, ActorChangeModeRequest message)
        {
            throw new NotImplementedException();
        }
        private void _HandleActorChangeModeResponse(Connection conn, ActorChangeModeResponse message)
        {
            throw new NotImplementedException();
        }

        private void _HandleActorChangeStateRequest(Connection conn, ActorChangeStateRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            ServersMgr.Instance.SendToSceneServer(session.curSceneId, message);
        End:
            return;
        }
        private void _HandleActorChangeStateResponse(Connection conn, ActorChangeStateResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if(session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

        private void _HandleActorChangeMotionDataRequest(Connection conn, ActorChangeMotionDataRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            ServersMgr.Instance.SendToSceneServer(session.curSceneId, message);
        End:
            return;
        }
        private void _HandleActorChangeMotionDataResponse(Connection conn, ActorChangeMotionDataResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }

    }
}
