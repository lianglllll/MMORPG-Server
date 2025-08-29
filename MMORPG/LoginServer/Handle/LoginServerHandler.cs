using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Login;
using LoginServer.Core;
using LoginServer.Net;
using Serilog;

namespace LoginServer.Handle
{
    public class LoginServerHandler : Singleton<LoginServerHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Instance.Register<RegisterToLRequest>((int)LoginProtocl.RegisterToLReq);
            ProtoHelper.Instance.Register<RegisterToLResponse>((int)LoginProtocl.RegisterToLResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<RegisterToLRequest>(_HandleRegisterToLRequest);
        }

        public void UnInit()
        {

        }

        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.DbproxyEnter)
            {
                Log.Debug("A new DBProxy Server has joined the cluster.");
                ServersMgr.Instance.AddDBServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
            else if (message.ClusterEventNode.EventType == ClusterEventType.GamegatemgrEnter)
            {
                Log.Debug("A new GGM Server has joined the cluster.");
                ServersMgr.Instance.AddGGMServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }

        private void _HandleRegisterToLRequest(Connection conn, RegisterToLRequest message)
        {
            RegisterToLResponse resp = new();
            resp.LoginToken = LoginServerMonitor.Instance.RegisterLoginGateInstance(conn, message.ServerInfoNode);
            resp.ResultCode = 0;
            conn.Send(resp);
        }

    }
}