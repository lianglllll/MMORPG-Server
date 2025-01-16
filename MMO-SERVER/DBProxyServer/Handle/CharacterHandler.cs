using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Core;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.DBProxy.DBUser;

namespace DBProxyServer.Handle
{
    public class CharacterHandler : Singleton<CharacterHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetDBCharacterByCidRequest>((int)DBCharacterProtocl.GetDbcharacterByCidReq);
            ProtoHelper.Instance.Register<GetDBCharacterByCidReponse>((int)DBCharacterProtocl.GetDbcharacterByCidResp);
            ProtoHelper.Instance.Register<AddDBCharacterRequset>((int)DBCharacterProtocl.AddDbcharacterReq);
            ProtoHelper.Instance.Register<AddDBCharacterResponse>((int)DBCharacterProtocl.AddDbcharacterResp);
            ProtoHelper.Instance.Register<DeleteDBCharacterByCidRequest>((int)DBCharacterProtocl.DeleteDbcharacterByCidReq);
            ProtoHelper.Instance.Register<DeleteDBCharacterByCidResponse>((int)DBCharacterProtocl.DeleteDbcharacterByCidResp);
            ProtoHelper.Instance.Register<GetDBCharactersByUidRequest>((int)DBCharacterProtocl.GetDbcharactersByUidReq);
            ProtoHelper.Instance.Register<GetDBCharactersByUidResponse>((int)DBCharacterProtocl.GetDbcharactersByUidResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetDBCharacterByCidRequest>(_HandleGetDBCharacterRequest);
            MessageRouter.Instance.Subscribe<AddDBCharacterRequset>(_HandleAddDBCharacterRequset);
            MessageRouter.Instance.Subscribe<DeleteDBCharacterByCidRequest>(_HandleDeleteDBCharacterRequest);
            MessageRouter.Instance.Subscribe<GetDBCharactersByUidRequest>(_HandleGetDBCharactersByUidRequest);
        }
        public async void _HandleGetDBCharacterRequest(Connection sender, GetDBCharacterByCidRequest message)
        {
            GetDBCharacterByCidReponse resp = new();
            resp.TaskId = message.TaskId;
            DBCharacterNode cNode = await CharacterOperations.Instance.GetCharacterByCidAsync(message.CId, message.ReadMask);
            if (cNode != null)
            {
                resp.ChrNode = cNode;
            }
            resp.ResultCode = 0;
            sender.Send(resp);
        }
        public async void _HandleAddDBCharacterRequset(Connection sender, AddDBCharacterRequset message)
        {
            AddDBCharacterResponse resp = new();
            string cId = await CharacterOperations.Instance.AddCharacterAsync(message.ChrNode);
            if (cId != null)
            {
                resp.CId = cId;
                resp.ResultCode = 0;
            }
            else
            {
                resp.ResultCode = 1;
            }

            sender.Send(resp);
        }
        private async void _HandleDeleteDBCharacterRequest(Connection sender, DeleteDBCharacterByCidRequest message)
        {
            DeleteDBUserResponse resp = new();

            bool successs = await CharacterOperations.Instance.DeleteCharacterByCidAsync(message.CId);
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
        private async void _HandleGetDBCharactersByUidRequest(Connection conn, GetDBCharactersByUidRequest message)
        {
            GetDBCharactersByUidResponse resp = new();
            resp.TaskId = message.TaskId;
            var list = await CharacterOperations.Instance.GetCharactersByUidAsync(message.UId, message.ReadMask);
            if(list != null)
            {
                resp.CNodes.AddRange(list);
            }
            resp.ResultCode = 0;
            conn.Send(resp);
        }
    }
}
