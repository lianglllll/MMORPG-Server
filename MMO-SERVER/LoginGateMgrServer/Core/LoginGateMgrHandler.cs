using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
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
            ProtoHelper.Register<GetAllServerInfoRequest>((int)ControlCenterProtocl.GetAllserverinfoReq);
            ProtoHelper.Register<GetAllServerInfoResponse>((int)ControlCenterProtocl.GetAllserverinfoResp);
            ProtoHelper.Register<RegisterLoginGateInstanceRequest>((int)LoginGateMgrProtocl.RegisterLogingateInstanceReq);
            ProtoHelper.Register<RegisterLoginGateInstanceResponse>((int)LoginGateMgrProtocl.RegisterLogingateInstanceResp);


            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllServerInfoResponse>(_HandleGetAllServerInfoResponse);
            MessageRouter.Instance.Subscribe<RegisterLoginGateInstanceResponse>(_HandleRegisterLoginGateInstanceResponse);

        }

        public void UnInit()
        {

        }

        public void SendGetAllServerInfoRequest()
        {
            var req = new GetAllServerInfoRequest();
            req.ServerType = SERVER_TYPE.Login; 
            ServersMgr.Instance.ccClient.Send(req);
        }
        private void _HandleGetAllServerInfoResponse(Connection sender, GetAllServerInfoResponse message)
        {
            if(message.ServerType == SERVER_TYPE.Login)
            {
                LogingateMonitor.Instance.UpdateLoginServerInfo(message.ServerInfoNodes.ToList());
                Log.Debug(message.ToString());
            }

        }

        private void _HandleRegisterLoginGateInstanceResponse(Connection sender, RegisterLoginGateInstanceResponse message)
        {
            throw new NotImplementedException();
        }

    }
}
