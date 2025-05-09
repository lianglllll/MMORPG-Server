using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core.Model;
using GameServer.Net;
using HS.Protobuf.Chat;
using HS.Protobuf.GameTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Hanle
{
    public class TaskHandler : Singleton<TaskHandler>
    {
        public override void Init()
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
            MessageRouter.Instance.Subscribe<TakeGameTaskRequest>(HandleTakeGameTaskRequest);
            MessageRouter.Instance.Subscribe<ReTakeGameTaskRequest>(HandleReTakeGameTaskRequest);
            MessageRouter.Instance.Subscribe<ClaimTaskRewardsRequest>(HandleClaimTaskRewardsRequest);
        }

        private void HandleGetAllGameTasksRequest(Connection conn, GetAllGameTasksRequest message)
        {
            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }

            var resp = new GetAllGameTaskResponse();
            resp.SessionId = message.SessionId;
            var tasks = chr.GameTaskManager.GetAllActiveTask();
            resp.Tasks.AddRange(tasks);

            conn.Send(resp);
        End:
            return;
        }
        private void HandleTakeGameTaskRequest(Connection conn, TakeGameTaskRequest message)
        {
            var chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if(chr == null)
            {
                goto End;
            }
            chr.GameTaskManager.TakeGameTask(message.TaskId);
        End:
            return;
        }
        private void HandleReTakeGameTaskRequest(Connection conn, ReTakeGameTaskRequest message)
        {
            var chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }
            chr.GameTaskManager.ReTakeGameTask(message.TaskId);
        End:
            return;
        }
        private void HandleClaimTaskRewardsRequest(Connection conn, ClaimTaskRewardsRequest message)
        {
            var chr = GameCharacterManager.Instance.GetGameCharacterByCid(message.CId);
            if (chr == null)
            {
                goto End;
            }
            chr.GameTaskManager.ClaimTaskRewards(message.TaskId);
        End:
            return;
        }
    }
}
