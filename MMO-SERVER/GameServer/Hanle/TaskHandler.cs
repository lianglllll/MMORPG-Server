using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core.Model;
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
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllGameTasksRequest>(HandleGetAllGameTasksRequest);
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
    }
}
