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
            ProtoHelper.Instance.Register<GameTaskChangeOperationResponse>((int)GameTaskProtocol.GameTaskChangeOperationResp);
            ProtoHelper.Instance.Register<TakeGameTaskRequest>((int)GameTaskProtocol.TakeGameTasksReq);
            ProtoHelper.Instance.Register<ReTakeGameTaskRequest>((int)GameTaskProtocol.ReTakeGameTasksReq);
            ProtoHelper.Instance.Register<ClaimTaskRewardsRequest>((int)GameTaskProtocol.ClaimTaskRewardsReq);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllGameTasksRequest>(HandleGetAllGameTasksRequest);
            MessageRouter.Instance.Subscribe<GetAllGameTaskResponse>(HandleGetAllGameTaskResponse);
            MessageRouter.Instance.Subscribe<GameTaskChangeOperationResponse>(HandleGameTaskChangeOperationResponse);
            MessageRouter.Instance.Subscribe<TakeGameTaskRequest>(HandleTakeGameTaskRequest);
            MessageRouter.Instance.Subscribe<ReTakeGameTaskRequest>(HandleReTakeGameTaskRequest);
            MessageRouter.Instance.Subscribe<ClaimTaskRewardsRequest>(HandleClaimTaskRewardsRequest);

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
        private void HandleGameTaskChangeOperationResponse(Connection conn, GameTaskChangeOperationResponse message)
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
        private void HandleTakeGameTaskRequest(Connection conn, TakeGameTaskRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleReTakeGameTaskRequest(Connection conn, ReTakeGameTaskRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }
        private void HandleClaimTaskRewardsRequest(Connection conn, ClaimTaskRewardsRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }
            message.SessionId = session.Id;
            message.CId = session.m_cId;
            ServersMgr.Instance.SendToGameServer(message);
        End:
            return;
        }

    }
}
