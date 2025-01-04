using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGate;
using LoginGateServer.Net;
using Serilog;

namespace LoginGateServer.Core
{
    public class LoginGateHandler : Singleton<LoginGateHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Register<GetLoginGateTokenRequest>((int)LoginGateProtocl.GetLogingateTokenReq);
            ProtoHelper.Register<GetLoginGateTokenResponse>((int)LoginGateProtocl.GetLogingateTokenResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<GetLoginGateTokenRequest>(_HandleGetLoginGateTokenRequest);
        }


        public void UnInit()
        {
        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if(message.ClusterEventNode.EventType == ClusterEventType.LogingatemgrEnter)
            {
                Log.Debug("A new LoginGateMgr server has joined the cluster.");
                ServersMgr.Instance.AddLGMServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }
        private void _HandleGetLoginGateTokenRequest(Connection conn, GetLoginGateTokenRequest message)
        {
            string tokenId = conn.Get<LoginGateToken>().Id;
            GetLoginGateTokenResponse resp = new GetLoginGateTokenResponse();
            resp.LoginGateToken = tokenId;
            conn.Send(resp);
        }
    }
}
