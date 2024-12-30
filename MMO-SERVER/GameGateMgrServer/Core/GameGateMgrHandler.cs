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
            ProtoHelper.Register<GetAllServerInfoRequest>((int)ControlCenterProtocl.GetAllserverinfoReq);
            ProtoHelper.Register<GetAllServerInfoResponse>((int)ControlCenterProtocl.GetAllserverinfoResp);
            ProtoHelper.Register<RegisterGameGateInstanceRequest>((int)GameGateMgrProtocl.RegisterGgInstanceReq);
            ProtoHelper.Register<RegisterGameGateInstanceResponse>((int)GameGateMgrProtocl.RegisterGgInstanceResp);
            ProtoHelper.Register<ExecuteGGCommandRequest>((int)GameGateMgrProtocl.ExecuteGgCommandReq);
            ProtoHelper.Register<ExecuteGGCommandResponse>((int)GameGateMgrProtocl.ExecuteGgCommandResp);
            ProtoHelper.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllServerInfoResponse>(_HandleGetAllServerInfoResponse);
            MessageRouter.Instance.Subscribe<RegisterGameGateInstanceRequest>(_HandleRegisterGameGateInstanceRequest);
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
        private void _HandleGetAllServerInfoResponse(Connection conn, GetAllServerInfoResponse message)
        {
            if(message.ServerType == SERVER_TYPE.Game)
            {
                GameGateMonitor.Instance.InitGameServerInfo(message.ServerInfoNodes.ToList());
                // Log.Debug(message.ToString());
            }

        }
        private void _HandleRegisterGameGateInstanceRequest(Connection conn, RegisterGameGateInstanceRequest message)
        {
            bool success = GameGateMonitor.Instance.RegisterGameGateInstance(conn ,message.ServerInfoNode);
            RegisterGameGateInstanceResponse resp = new();
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
            if(message.EventType == ClusterEventType.GameEnter)
            {
                Log.Debug("A new Game server has joined the cluster.");
                GameGateMonitor.Instance.AddGameServerInfo(message.ServerInfoNode);
            }
            else if (message.EventType == ClusterEventType.GameExit)
            {
                Log.Debug("A Game server has left the cluster.");
                GameGateMonitor.Instance.RemoveGameServerInfo(message.ServerId);
            }
            else
            {
                Log.Debug("Unknown ccEvent");
            }
        }
    }
}
