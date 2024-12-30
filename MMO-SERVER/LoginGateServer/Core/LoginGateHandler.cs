using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGateMgr;
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
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
        }
        public void UnInit()
        {


        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if(message.EventType == ClusterEventType.LogingatemgrEnter)
            {
                Log.Debug("A new LoginGateMgr server has joined the cluster.");
                ServersMgr.Instance.SetLGMAndConnect(message.ServerInfoNode);
            }
        }
    }
}
