using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Login;
using LoginServer.Net;
using Serilog;
using System.Collections;

namespace LoginServer.Core
{
    public class LoginServerHandler : Singleton<LoginServerHandler>
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
            if (message.ClusterEventNode.EventType == ClusterEventType.DbproxyEnter)
            {
                Log.Debug("A new DBProxy Server has joined the cluster.");
                ServersMgr.Instance.AddDBServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }


    }
}