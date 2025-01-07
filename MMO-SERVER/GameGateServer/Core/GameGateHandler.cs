using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
using GameGateServer.Net;
using Serilog;

namespace GameGateServer.Core
{
    public class GameGateHandler : Singleton<GameGateHandler>
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
            if(message.ClusterEventNode.EventType == ClusterEventType.GamegatemgrEnter)
            {
                Log.Debug("A new GameGateMgr server has joined the cluster.");
                ServersMgr.Instance.AddGGMServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }
    }
}
