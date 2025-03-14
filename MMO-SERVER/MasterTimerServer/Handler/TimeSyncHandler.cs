using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Login;
using HS.Protobuf.MasterTime;
using MasterTimerServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterTimerServer.Handler
{
    public class TimeSyncHandler : Singleton<TimeSyncHandler>
    {
        public override void Init()
        {
            ProtoHelper.Instance.Register<TimeSyncRequest>((int)MasterTimeProtocl.TimeSyncReq);
            ProtoHelper.Instance.Register<TimeSyncResponse>((int)MasterTimeProtocl.TimeSyncResp);
            MessageRouter.Instance.Subscribe<TimeSyncRequest>(_HandleTimeSyncRequest);
        }

        private void _HandleTimeSyncRequest(Connection conn, TimeSyncRequest message)
        {
            // 接收请求时记录精确时间戳
            var receiveTime = TimeMonitor.Instance.GetUnixTimeMilliseconds;

            // 构建响应包（包含三级时间标记）
            var resp = new TimeSyncResponse
            {
                ClientSendTime = message.ClientSendTime,
                ServerReceiveTime   = receiveTime,
                ServerSendTime      = TimeMonitor.Instance.GetUnixTimeMilliseconds  // 发送响应时间
            };

            conn.Send(resp);
        }
    }
}
