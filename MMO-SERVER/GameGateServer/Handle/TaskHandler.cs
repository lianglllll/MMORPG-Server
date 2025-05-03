using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using HS.Protobuf.Chat;
using HS.Protobuf.GameTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameGateServer.Handle
{
    public class TaskHandler : Singleton<TaskHandler>
    {
        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetAllGameTasksRequest>((int)GameTaskProtocol.GetAllGameTasksReq);
            ProtoHelper.Instance.Register<GetAllGameTaskResponse>((int)GameTaskProtocol.GetAllGameTasksResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllGameTasksRequest>(HandleGetAllGameTasksRequest);
            MessageRouter.Instance.Subscribe<GetAllGameTaskResponse>(HandleGetAllGameTaskResponse);

            return true;
        }

        private void HandleGetAllGameTasksRequest(Connection conn, GetAllGameTasksRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }

        private void HandleGetAllGameTaskResponse(Connection conn, GetAllGameTaskResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }
    }
}
