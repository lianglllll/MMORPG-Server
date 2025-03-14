using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Net;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Game;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBProxyServer.Handle
{
    public class DBProxyServerHandler : Singleton<DBProxyServerHandler>
    {
        public override void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
        }

        private void _HandleClusterEventResponse(Connection conn, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.MastertimeEnter)
            {
                Log.Debug("A new MT server has joined the cluster, {0}", message.ClusterEventNode.ServerInfoNode);
                DBProxyServersMgr.Instance.AddMTServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }
    }
}
