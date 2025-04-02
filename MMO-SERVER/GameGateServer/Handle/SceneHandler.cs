using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using HS.Protobuf.Combat.Skill;
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
            ProtoHelper.Instance.Register<ActorChangeTransformDataRequest>((int)SceneProtocl.ActorChangeTransformDataReq);
            ProtoHelper.Instance.Register<ActorChangeTransformDataResponse>((int)SceneProtocl.ActorChangeTransformDataResp);
            ProtoHelper.Instance.Register<SpellCastRequest>((int)SkillProtocol.SpellCastReq);
            ProtoHelper.Instance.Register<SpellCastResponse>((int)SkillProtocol.SpellCastResp);
            ProtoHelper.Instance.Register<SpellCastFailResponse>((int)SkillProtocol.SpellCastFailResp);

            ProtoHelper.Instance.Register<Scene2GateMsg>((int)SceneProtocl.Scene2GateMsg);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<OtherEntityEnterSceneResponse>(_HandleOtherEntityEnterSceneResponse);
            MessageRouter.Instance.Subscribe<OtherEntityLeaveSceneResponse>(_HandleOtherEntityLeaveSceneResponse);
            MessageRouter.Instance.Subscribe<ActorChangeModeRequest>(_HandleActorChangeModeRequest);
            MessageRouter.Instance.Subscribe<ActorChangeModeResponse>(_HandleActorChangeModeResponse);
            MessageRouter.Instance.Subscribe<ActorChangeStateRequest>(_HandleActorChangeStateRequest);
            MessageRouter.Instance.Subscribe<ActorChangeStateResponse>(_HandleActorChangeStateResponse);
            MessageRouter.Instance.Subscribe<ActorChangeTransformDataRequest>(_HandleActorChangeTransformDataRequest);
            MessageRouter.Instance.Subscribe<ActorChangeTransformDataResponse>(_HandleActorChangeTransformDataResponse);

            MessageRouter.Instance.Subscribe<SpellCastRequest>(HandleSpellCastRequest);
            MessageRouter.Instance.Subscribe<SpellCastResponse>(HandleSpellCastResponse);
            MessageRouter.Instance.Subscribe<SpellCastFailResponse>(HandleSpellFailResponse);

            MessageRouter.Instance.Subscribe<Scene2GateMsg>(HandleScene2GateMsg);

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
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            ServersMgr.Instance.SendToSceneServer(session.curSceneId, message);
        End:
            return;
        }
        private void _HandleActorChangeModeResponse(Connection conn, ActorChangeModeResponse message)
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

        private void _HandleActorChangeTransformDataRequest(Connection conn, ActorChangeTransformDataRequest message)
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
        private void _HandleActorChangeTransformDataResponse(Connection conn, ActorChangeTransformDataResponse message)
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

        private void HandleSpellCastRequest(Connection conn, SpellCastRequest message)
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
        private void HandleSpellCastResponse(Connection conn, SpellCastResponse message)
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
        private void HandleSpellFailResponse(Connection conn, SpellCastFailResponse message)
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

        private void HandleScene2GateMsg(Connection conn, Scene2GateMsg message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message.Content);
        End:
            return;
        }

    }
}
