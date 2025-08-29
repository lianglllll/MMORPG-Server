using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;
using GameGateServer.Net;
using Serilog;
using HS.Protobuf.GameGate;

namespace GameGateServer.Handle
{
    public class GameGateServerHandler : Singleton<GameGateServerHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Instance.Register<RegisterSceneToGGRequest>((int)GameGateProtocl.RegisterScenesToGgReq);
            ProtoHelper.Instance.Register<RegisterSceneToGGResponse>((int)GameGateProtocl.RegisterScenesToGgResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<RegisterSceneToGGRequest>(_HandleRegisterSceneToGGRequest);
        }

        public void UnInit()
        {
        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.GamegatemgrEnter)
            {
                Log.Information("A new GameGateMgr server has joined the cluster, {0}", message.ClusterEventNode.ServerInfoNode);
                ServersMgr.Instance.AddGGMServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }
        private void _HandleRegisterSceneToGGRequest(Connection conn, RegisterSceneToGGRequest message)
        {
            foreach(var node in message.SceneInfos)
            {
                ServersMgr.Instance.AddSServerInfo(node);
            }
        }
    }
}
