using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core;
using GameServer.Net;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Game;
using Serilog;
using System;

namespace GameServer.Handle
{
    public class GameServerHandler : Singleton<GameServerHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Instance.Register<RegisterToGRequest>((int)GameProtocl.RegisterToGReq);
            ProtoHelper.Instance.Register<RegisterToGResponse>((int)GameProtocl.RegisterToGResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<RegisterToGRequest>(_HandleRegisterToGRequest);
        
        
        }

        public void UnInit()
        {
        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.DbproxyEnter)
            {
                Log.Debug("A new DBPorxy server has joined the cluster.");
                ServersMgr.Instance.AddDBServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }

        private void _HandleRegisterToGRequest(Connection conn, RegisterToGRequest message)
        {
            GameMonitor.Instance.RegisterInstance(conn, message.ServerInfoNode);
        }

    }
}
