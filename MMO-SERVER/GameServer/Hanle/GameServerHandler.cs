using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core;
using GameServer.Net;
using HS.Protobuf.Common;
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
                Log.Debug("A new DBPorxy server has joined the cluster, {0}", message.ClusterEventNode.ServerInfoNode);
                ServersMgr.Instance.AddDBServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }
        private void _HandleRegisterToGRequest(Connection conn, RegisterToGRequest message)
        {
            if (message.ServerInfoNode.ServerType == SERVER_TYPE.Gamegate)
            {
                Log.Information("GameGate rigister {0}", message.ServerInfoNode);
            }
            else if (message.ServerInfoNode.ServerType == SERVER_TYPE.Scene)
            {
                Log.Information("Scene rigister {0}", message.ServerInfoNode);
            }
            bool success = GameMonitor.Instance.RegisterInstance(conn, message.ServerInfoNode);
        }
    }
}
