using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
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

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
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
        }

    }
}