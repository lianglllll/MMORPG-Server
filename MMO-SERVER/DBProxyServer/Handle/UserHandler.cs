using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Core;
using HS.Protobuf.DBProxy.DBUser;
using MongoDB.Bson;

namespace DBProxyServer.Handle
{
    public class UserHandler:Singleton<UserHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetDBUserRequest>((int)DBUserProtocl.GetDbuserReq);
            ProtoHelper.Instance.Register<GetDBUserResponse>((int)DBUserProtocl.GetDbuserResp);
            ProtoHelper.Instance.Register<AddDBUserRequset>((int)DBUserProtocl.AddDbuserReq);
            ProtoHelper.Instance.Register<AddDBUserResponse>((int)DBUserProtocl.AddDbuserResp);
            ProtoHelper.Instance.Register<UpdateDBUserPasswordRequest>((int)DBUserProtocl.UpdateDbuserPasswordReq);
            ProtoHelper.Instance.Register<UpdateDBUserPasswordResponse>((int)DBUserProtocl.UpdateDbuserPasswordResp);
            ProtoHelper.Instance.Register<DeleteDBUserRequest>((int)DBUserProtocl.DeleteDbuserReq);
            ProtoHelper.Instance.Register<DeleteDBUserResponse>((int)DBUserProtocl.DeleteDbuserResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetDBUserRequest>(_HandleGetDBUserRequest);
            MessageRouter.Instance.Subscribe<AddDBUserRequset>(_HandleAddDBUserRequset);
            MessageRouter.Instance.Subscribe<UpdateDBUserPasswordRequest>(_HandleUpdateDBUserPasswordRequest);
            MessageRouter.Instance.Subscribe<DeleteDBUserRequest>(_HandleDeleteDBUserRequest);
        }
        public async void _HandleGetDBUserRequest(Connection sender, GetDBUserRequest message)
        {
            GetDBUserResponse resp = new();
            resp.TaskId = message.TaskId;

            DBUserNode dBUserNode = null;
            if (!string.IsNullOrEmpty(message.UId))
            {
                dBUserNode = await UserOperations.Instance.GetDBUserByUidAsync(message.UId);
            }
            else if(!string.IsNullOrEmpty(message.UserName))
            {
                dBUserNode = await UserOperations.Instance.GetDBUserByNameAsync(message.UserName);
            }

            if (dBUserNode == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = $"No information found for the username {message.UserName}.";
                goto End;
            }
            else
            {
                resp.User = dBUserNode;
                resp.ResultCode = 0;
            }

        End:
            sender.Send(resp);
        }
        public async void _HandleAddDBUserRequset(Connection sender, AddDBUserRequset message)
        {
            AddDBUserResponse resp = new();
            resp.TaskId = message.TaskId;

            // 先找找有没有重复名，有就直接拒绝他
            DBUserNode existNode = await UserOperations.Instance.GetDBUserByNameAsync(message.DbUserNode.UserName);
            if(existNode != null)
            {
                resp.ResultCode = 1;
                goto End;
            }

            // 填一些默认字段
            DBUserNode dBUserNode = message.DbUserNode;
            dBUserNode.AccountStatus = "active";
            dBUserNode.AccessLevel = "user";
            dBUserNode.CreationTimestamp = Scheduler.UnixTime;

            bool success = await UserOperations.Instance.AddUserAsync(dBUserNode);
            if (success)
            {
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultMsg = "未知错误，请联系管理员";
                resp.ResultCode = 2;
            }

         End:
            sender.Send(resp);   
        }
        private async void _HandleUpdateDBUserPasswordRequest(Connection sender, UpdateDBUserPasswordRequest message)
        {
            UpdateDBUserPasswordResponse resp = new();

            bool successs = await UserOperations.Instance.UpdatePasswordAsync(message.UId, message.NewPassword);
            if (successs)
            {
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultCode = 1;
            }

            sender.Send(resp);

        }
        private async void _HandleDeleteDBUserRequest(Connection sender, DeleteDBUserRequest message)
        {
            DeleteDBUserResponse resp = new();

            bool successs = await CharacterOperations.Instance.RemoveCharactersByUidAsync(message.UId);
            if (successs)
            {
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultCode = 1;
            }

            sender.Send(resp);
        }
    }
}
