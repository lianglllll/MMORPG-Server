using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using GameGateMgrServer.Net;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.GameGateMgr;
using Serilog;

namespace GameGateMgrServer.Core
{
    public class GameGateMgrHandler:Singleton<GameGateMgrHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Register<RegisterToGGMRequest>((int)GameGateMgrProtocl.RegisterToGgmReq);
            ProtoHelper.Register<RegisterToGGMResponse>((int)GameGateMgrProtocl.RegisterToGgmResp);
            ProtoHelper.Register<ExecuteGGCommandRequest>((int)GameGateMgrProtocl.ExecuteGgCommandReq);
            ProtoHelper.Register<ExecuteGGCommandResponse>((int)GameGateMgrProtocl.ExecuteGgCommandResp);
            ProtoHelper.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<RegisterToGGMRequest>(_HandleRegisterToGGMRequest);
            MessageRouter.Instance.Subscribe<ExecuteGGCommandResponse>(_HandleExecuteGGCommandResponse);
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);

        }

        public void UnInit()
        {

        }

        public void SendGetAllGameServerInfoRequest()
        {
            var req = new GetAllServerInfoRequest();
            req.ServerType = SERVER_TYPE.Game; 
            ServersMgr.Instance.ccClient.Send(req);
        }
        private void _HandleRegisterToGGMRequest(Connection conn, RegisterToGGMRequest message)
        {
            bool success = GameGateMonitor.Instance.RegisterToGGMInstance(conn ,message.ServerInfoNode);
            RegisterToGGMResponse resp = new();
            if (success)
            {
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "RegisterGameGateInstance failed";
            }
            conn.Send(resp);
        }
        private void _HandleExecuteGGCommandResponse(Connection sender, ExecuteGGCommandResponse message)
        {
            if(message.ResultCode == 0)
            {
            }
            else
            {
                Log.Error("ExecuteLGCommandResponse failed");
            }
        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if(message.ClusterEventNode.EventType == ClusterEventType.GameEnter)
            {
                Log.Debug("A new Game server has joined the cluster.");
                GameGateMonitor.Instance.AddGameServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
            else if (message.ClusterEventNode.EventType == ClusterEventType.GameExit)
            {
                Log.Debug("A Game server has left the cluster.");
                GameGateMonitor.Instance.RemoveGameServerInfo(message.ClusterEventNode.ServerId);
            }
            else
            {
                Log.Debug("Unknown ccEvent");
            }
        }
    }
}
