using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using Google.Protobuf;
using HS.Protobuf.Game;
using HS.Protobuf.GameGate;

namespace GameGateServer.Handle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();

        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<RegisterSessionToGGRequest>((int)GameGateProtocl.RegisterSessionToGgReq);
            ProtoHelper.Instance.Register<RegisterSessionToGGResponse>((int)GameGateProtocl.RegisterSessionToGgResp);
            ProtoHelper.Instance.Register<VerifySessionRequeest>((int)GameGateProtocl.VerifySessionReq);
            ProtoHelper.Instance.Register<VerifySessionResponse>((int)GameGateProtocl.VerifySessionResp);

            ProtoHelper.Instance.Register<GetCharacterListRequest>((int)GameProtocl.GetCharacterListReq);
            ProtoHelper.Instance.Register<GetCharacterListResponse>((int)GameProtocl.GetCharacterListResp);
            ProtoHelper.Instance.Register<CreateCharacterRequest>((int)GameProtocl.CreateCharacterReq);
            ProtoHelper.Instance.Register<CreateCharacterResponse>((int)GameProtocl.CreateCharacterResp);
            ProtoHelper.Instance.Register<DeleteCharacterRequest>((int)GameProtocl.DeleteCharacterReq);
            ProtoHelper.Instance.Register<DeleteCharacterResponse>((int)GameProtocl.DeleteCharacterResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<RegisterSessionToGGRequest>(_HandleRegisterSessionToGGRequest);
            MessageRouter.Instance.Subscribe<VerifySessionRequeest>(_HandleVerifySessionRequeest);

            MessageRouter.Instance.Subscribe<GetCharacterListRequest>(_HandleGetCharacterListRequest);
            MessageRouter.Instance.Subscribe<GetCharacterListResponse>(_HandleGetCharacterListResponse);
            MessageRouter.Instance.Subscribe<CreateCharacterRequest>(_HandleCreateCharacterRequest);
            MessageRouter.Instance.Subscribe<CreateCharacterResponse>(_HandleCreateCharacterResponse);
            MessageRouter.Instance.Subscribe<DeleteCharacterRequest>(_HandleDeleteCharacterRequest);
            MessageRouter.Instance.Subscribe<DeleteCharacterResponse>(_HandleDeleteCharacterResponse);

            return true;
        }


        private void _HandleRegisterSessionToGGRequest(Connection conn, RegisterSessionToGGRequest message)
        {
            SessionManager.Instance.NewSession(message.SessionId);
        }
        private void _HandleVerifySessionRequeest(Connection conn, VerifySessionRequeest message)
        {
            VerifySessionResponse resp = new();

            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if(session == null)
            {
                // 可能是未登录访问，拒绝
                resp.ResultCode = 1;
                resp.ResultMsg = "未登录访问!";
                conn.Send(resp);
                Scheduler.Instance.AddTask(() => {
                    NetService.Instance.CloseUserConnection(conn);
                },1,1,1);
                goto End;
            }

            // 关联session 和 conn
            session.Conn = conn;
            conn.Set<Session>(session);
            resp.ResultCode = 0;
            conn.Send(resp);
        End:
            return;
        }


        private void _HandleGetCharacterListRequest(Connection conn, GetCharacterListRequest message)
        {
        }
        private void _HandleGetCharacterListResponse(Connection conn, GetCharacterListResponse message)
        {
            throw new NotImplementedException();
        }

        private void _HandleCreateCharacterRequest(Connection conn, CreateCharacterRequest message)
        {
            throw new NotImplementedException();
        }
        private void _HandleCreateCharacterResponse(Connection conn, CreateCharacterResponse message)
        {
            throw new NotImplementedException();
        }
        private void _HandleDeleteCharacterRequest(Connection conn, DeleteCharacterRequest message)
        {
            throw new NotImplementedException();
        }
        private void _HandleDeleteCharacterResponse(Connection conn, DeleteCharacterResponse message)
        {
            throw new NotImplementedException();
        }

    }
}
