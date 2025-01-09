using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGate;
using LoginGateServer.Net;
using Serilog;

namespace LoginGateServer.Handle
{
    public class LoginGateHandler : Singleton<LoginGateHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Instance.Register<GetLoginGateTokenRequest>((int)LoginGateProtocl.GetLogingateTokenReq);
            ProtoHelper.Instance.Register<GetLoginGateTokenResponse>((int)LoginGateProtocl.GetLogingateTokenResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<GetLoginGateTokenRequest>(_HandleGetLoginGateTokenRequest);
        }

        public void UnInit()
        {
        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.LogingatemgrEnter)
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
