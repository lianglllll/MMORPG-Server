using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateMgrServer.Core;
using GameGateMgrServer.Net;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.GameGate;
using HS.Protobuf.GameGateMgr;
using Serilog;

namespace GameGateMgrServer.Handle
{
    public class GameGateMgrHandler : Singleton<GameGateMgrHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<RegisterToGGMRequest>((int)GameGateMgrProtocl.RegisterToGgmReq);
            ProtoHelper.Instance.Register<RegisterToGGMResponse>((int)GameGateMgrProtocl.RegisterToGgmResp);
            ProtoHelper.Instance.Register<ExecuteGGCommandRequest>((int)GameGateMgrProtocl.ExecuteGgCommandReq);
            ProtoHelper.Instance.Register<ExecuteGGCommandResponse>((int)GameGateMgrProtocl.ExecuteGgCommandResp);
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Instance.Register<RegisterSessionToGGMRequest>((int)GameGateMgrProtocl.RegisterSessoionToGgmReq);
            ProtoHelper.Instance.Register<RegisterSessionToGGMResponse>((int)GameGateMgrProtocl.RegisterSessoionToGgmResp);
            ProtoHelper.Instance.Register<RegisterSessionToGGRequest>((int)GameGateProtocl.RegisterSessionToGgReq);
            ProtoHelper.Instance.Register<RegisterSessionToGGResponse>((int)GameGateProtocl.RegisterSessionToGgResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<RegisterToGGMRequest>(_HandleRegisterToGGMRequest);
            MessageRouter.Instance.Subscribe<ExecuteGGCommandResponse>(_HandleExecuteGGCommandResponse);
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<RegisterSessionToGGMRequest>(_HandleRegisterSessionToGGMRequest);
        }
        public void UnInit()
        {

        }

        private void _HandleRegisterToGGMRequest(Connection conn, RegisterToGGMRequest message)
        {
            bool success = Core.GGMMonitor.Instance.RegisterToGGMInstance(conn, message.ServerInfoNode);
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
            if (message.ResultCode == 0)
            {
            }
            else
            {
                Log.Error("ExecuteLGCommandResponse failed");
            }
        }
        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.GameEnter)
            {
                Log.Debug("A new Game server has joined the cluster.");
                Core.GGMMonitor.Instance.AddGameServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
            else if (message.ClusterEventNode.EventType == ClusterEventType.GameExit)
            {
                Log.Debug("A Game server has left the cluster.");
                Core.GGMMonitor.Instance.RemoveGameServerInfo(message.ClusterEventNode.ServerId);
            }
            else
            {
                Log.Debug("Unknown ccEvent");
            }
        }
        private void _HandleRegisterSessionToGGMRequest(Connection conn, RegisterSessionToGGMRequest message)
        {
            RegisterSessionToGGMResponse resp = new();
            resp.TaskId = message.TaskId;
            List<ServerInfoNode> gameGateSIN = GGMMonitor.Instance.RegisterSession(message);
            if (gameGateSIN.Count == 0)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "目标世界缺失门户";
                goto End;
            }
            resp.ResultCode = 0;
            resp.GameGateInfos.AddRange(gameGateSIN);  // todo 拷贝重复。
        End:
            conn.Send(resp);
        }
    }
}
