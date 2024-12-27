
using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.ControlCenter;

namespace ControlCenter.Core
{
    public class ControlCenterHandler : Singleton<ControlCenterHandler>
    {
        public bool Init()
        {
            // 协议注册
            ProtoHelper.Register<GetAllServerInfoRequest>((int)ControlCenterProtocl.GetAllserverinfoReq);
            ProtoHelper.Register<GetAllServerInfoResponse>((int)ControlCenterProtocl.GetAllserverinfoResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<GetAllServerInfoRequest>(_HandleGetAllServerInfoRequest);

            return true;
        }

        public bool UnInit()
        {
            return true;
        }

        private void _HandleGetAllServerInfoRequest(Connection conn, GetAllServerInfoRequest message)
        {
            var resp = new GetAllServerInfoResponse();
            var list = ServersMgr.Instance.GetAllServerInfoByServerType(message.ServerType);
            resp.ServerType = message.ServerType;
            resp.ServerInfoNodes.AddRange(list);
            conn.Send(resp);
        }


    }
}
