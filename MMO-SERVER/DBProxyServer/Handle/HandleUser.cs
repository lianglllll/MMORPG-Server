using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using DBProxyServer.Core;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.DBProxy.DBUser;
using MongoDB.Bson;
using System.Linq;

namespace DBProxyServer.Handle
{
    public class HandleUser:Singleton<HandleUser>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Register<GetDBUserRequest>((int)DBUserProtocl.GetDbuserReq);
            ProtoHelper.Register<GetDBUserResponse>((int)DBUserProtocl.GetDbuserResp);
            ProtoHelper.Register<AddDBUserRequset>((int)DBUserProtocl.AddDbuserReq);
            ProtoHelper.Register<AddDBUserResponse>((int)DBUserProtocl.AddDbuserResp);
            ProtoHelper.Register<UpdateDBUserPasswordRequest>((int)DBUserProtocl.UpdateDbuserPasswordReq);
            ProtoHelper.Register<UpdateDBUserPasswordResponse>((int)DBUserProtocl.UpdateDbuserPasswordResp);
            ProtoHelper.Register<DeleteDBUserRequest>((int)DBUserProtocl.DeleteDbuserReq);
            ProtoHelper.Register<DeleteDBUserResponse>((int)DBUserProtocl.DeleteDbuserResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetDBUserRequest>(_HandleGetDBUserRequest);
            MessageRouter.Instance.Subscribe<AddDBUserRequset>(_HandleAddDBUserRequset);
            MessageRouter.Instance.Subscribe<UpdateDBUserPasswordRequest>(_HandleUpdateDBUserPasswordRequest);
            MessageRouter.Instance.Subscribe<DeleteDBUserRequest>(_HandleDeleteDBUserRequest);
        }

        public async void _HandleGetDBUserRequest(Connection sender, GetDBUserRequest message)
        {
            // Console.WriteLine($"Before await - Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            GetDBUserResponse resp = new();
            BsonDocument userDocument = await UserOperations.Instance.GetUserByNameAsync(message.UserName);
            // Console.WriteLine($"After await - Thread ID: {Thread.CurrentThread.ManagedThreadId}");
            if (userDocument == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = $"No information found for the username {message.UserName}.";
                goto End;
            }
            else
            {
                DBUserNode uNode = new();
                uNode.UId = userDocument["_id"].AsObjectId.ToString();
                uNode.UserName = userDocument["userName"].ToString();
                uNode.Password = userDocument["password"].ToString();
                if(userDocument.Contains("characterIds"))
                {
                    BsonArray chrIds = userDocument["characterIds"].AsBsonArray;
                    foreach (var chrId in chrIds)
                    {
                        uNode.CharacterIds.Add(chrId.ToString());
                    }
                }
                resp.User = uNode;
                resp.ResultCode = 0;
            }

        End:
            sender.Send(resp);
        }
        public async void _HandleAddDBUserRequset(Connection sender, AddDBUserRequset message)
        {
            AddDBUserResponse resp = new();
            DBUserNode userNode = new() { 
                UserName = message.UserName,
                Password = message.Password 
            };
            bool success = await UserOperations.Instance.AddUserAsync(userNode);
            if (success)
            {
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultCode = 1;
            }
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

            bool successs = await UserOperations.Instance.DeleteUserByUidAsync(message.UId);
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
