using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGateMgr;
using LoginGateMgrServer.Net;
using Serilog;

namespace LoginGateMgrServer.Core
{
    public class LoginGateMgrHandler:Singleton<LoginGateMgrHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<RegisterLoginGateInstanceRequest>((int)LoginGateMgrProtocl.RegisterLogingateInstanceReq);
            ProtoHelper.Instance.Register<RegisterLoginGateInstanceResponse>((int)LoginGateMgrProtocl.RegisterLogingateInstanceResp);
            ProtoHelper.Instance.Register<ExecuteLGCommandRequest>((int)LoginGateMgrProtocl.ExecuteLgCommandReq);
            ProtoHelper.Instance.Register<ExecuteLGCommandResponse>((int)LoginGateMgrProtocl.ExecuteLgCommandResp);
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<RegisterLoginGateInstanceRequest>(_HandleRegisterLoginGateInstanceRequest);
            MessageRouter.Instance.Subscribe<ExecuteLGCommandResponse>(_HandleExecuteLGCommandResponse);
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
        }

        public void UnInit()
        {

        }

        private void _HandleRegisterLoginGateInstanceRequest(Connection conn, RegisterLoginGateInstanceRequest message)
        {
            bool success = LogingateMonitor.Instance.RegisterLoginGateInstance(conn ,message.ServerInfoNode);
            RegisterLoginGateInstanceResponse resp = new();
            if (success)
            {
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "RegisterLoginGateInstance failed";
            }
            conn.Send(resp);
        }
        private void _HandleExecuteLGCommandResponse(Connection sender, ExecuteLGCommandResponse message)
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
            if(message.ClusterEventNode.EventType == ClusterEventType.LoginEnter)
            {
                Log.Debug("A new Login server has joined the cluster.");
                LogingateMonitor.Instance.AddLoginServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
            else if (message.ClusterEventNode.EventType == ClusterEventType.LoginExit)
            {
                Log.Debug("A Login server has left the cluster.");
                LogingateMonitor.Instance.RemoveLoginServerInfo(message.ClusterEventNode.ServerId);
            }
            else
            {
                Log.Debug("Unknown ccEvent");
            }
        }
    }
}
