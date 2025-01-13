using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Security;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBWorld;
using HS.Protobuf.Login;
using LoginGateServer.Net;

namespace LoginGateServer.Handle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();

        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetAllWorldInfosRequest>((int)LoginProtocl.GetAllWorldInfoNodeReq);
            ProtoHelper.Instance.Register<GetAllWorldInfosResponse>((int)LoginProtocl.GetAllWorldInfoNodeResp);
            ProtoHelper.Instance.Register<GetGameGateByWorldIdRequest>((int)LoginProtocl.GetGameGateByWorldidReq);
            ProtoHelper.Instance.Register<GetGameGateByWorldIdResponse>((int)LoginProtocl.GetGameGateByWorldidResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllWorldInfosRequest>(_HandleGetAllWorldInfosRequest);
            MessageRouter.Instance.Subscribe<GetAllWorldInfosResponse>(_HandleGetAllWorldInfosResponse);
            MessageRouter.Instance.Subscribe<GetGameGateByWorldIdRequest>(_HandleGetGameGateByWorldIdRequest);
            MessageRouter.Instance.Subscribe<GetGameGateByWorldIdResponse>(_HandleGetGameGateByWorldIdResponse);
            return true;
        }

        public bool UnInit()
        {
            return true;
        }

        private void _HandleGetAllWorldInfosRequest(Connection conn, GetAllWorldInfosRequest message)
        {
            // 转发到loginServer
            message.LoginToken = ServersMgr.Instance.LoginToken;
            ServersMgr.Instance.SentToLoginServer(message);
        }
        private void _HandleGetAllWorldInfosResponse(Connection conn, GetAllWorldInfosResponse message)
        {
            LoginGateToken token = LoginGateTokenManager.Instance.GetToken(message.LoginGateToken);
            message.LoginGateToken = "";
            token.Send(message);
        }

        private void _HandleGetGameGateByWorldIdRequest(Connection conn, GetGameGateByWorldIdRequest message)
        {
            // 转发到loginServer
            message.LoginToken = ServersMgr.Instance.LoginToken;
            ServersMgr.Instance.SentToLoginServer(message);
        }
        private void _HandleGetGameGateByWorldIdResponse(Connection conn, GetGameGateByWorldIdResponse message)
        {
            LoginGateToken token = LoginGateTokenManager.Instance.GetToken(message.LoginGateToken);
            message.LoginGateToken = "";
            token.Send(message);
        }

    }
}
