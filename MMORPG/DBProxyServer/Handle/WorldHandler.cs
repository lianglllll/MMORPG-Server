using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Core;
using HS.Protobuf.DBProxy.DBUser;
using HS.Protobuf.DBProxy.DBWorld;
using MongoDB.Bson;

namespace DBProxyServer.Handle
{
    public class WorldHandler : Singleton<WorldHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetDBWorldNodeByWorldIdRequest>((int)DBWorldProtocl.GetDbworldNodeByWorldidReq);
            ProtoHelper.Instance.Register<GetDBWorldNodeByWorldIdResponse>((int)DBWorldProtocl.GetDbworldNodeByWorldidResp);
            ProtoHelper.Instance.Register<GetAllDBWorldNodeRequest>((int)DBWorldProtocl.GetAllDbworldNodeReq);
            ProtoHelper.Instance.Register<GetAllDBWorldNodeResponse>((int)DBWorldProtocl.GetAllDbworldNodeResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetDBWorldNodeByWorldIdRequest>(_HandleGetDBWorldNodeByWorldIdRequest);
            MessageRouter.Instance.Subscribe<GetAllDBWorldNodeRequest>(_HandleGetAllDBWorldNodeRequest);
        }

        public async void _HandleGetDBWorldNodeByWorldIdRequest(Connection sender, GetDBWorldNodeByWorldIdRequest message)
        {
            GetDBWorldNodeByWorldIdResponse resp = new();
            resp.TaskId = message.TaskId;

            DBWorldNode dBWorldNode = await WorldOperations.Instance.GetDBWorldNodeByWorldIdAsync(message.WorldId);
            if (dBWorldNode == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = $"No information found for the worldId: {message.WorldId}.";
                goto End;
            }
            else
            {
                resp.DbWorldNode = dBWorldNode;
                resp.ResultCode = 0;
            }

        End:
            sender.Send(resp);
        }

        private async void _HandleGetAllDBWorldNodeRequest(Connection sender, GetAllDBWorldNodeRequest message)
        {
            GetAllDBWorldNodeResponse resp = new();
            resp.TaskId = message.TaskId;

            var worlds = await WorldOperations.Instance.GetAllWorldNodeAsync();
            if (worlds == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "Unopened World";
                goto End;
            }
            else
            {
                resp.ResultCode = 0;
                resp.Nodes.AddRange(worlds);
            }
        End:
            sender.Send(resp);
        }

    }
}
