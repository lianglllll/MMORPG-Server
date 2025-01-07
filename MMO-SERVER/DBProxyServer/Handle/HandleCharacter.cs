using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Core;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.DBProxy.DBUser;

namespace DBProxyServer.Handle
{
    public class HandleCharacter : Singleton<HandleCharacter>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetDBCharacterRequest>((int)DBCharacterProtocl.GetDbcharacterReq);
            ProtoHelper.Instance.Register<GetDBCharacterReponse>((int)DBCharacterProtocl.GetDbcharacterResp);
            ProtoHelper.Instance.Register<AddDBCharacterRequset>((int)DBCharacterProtocl.AddDbcharacterReq);
            ProtoHelper.Instance.Register<AddDBCharacterResponse>((int)DBCharacterProtocl.AddDbcharacterResp);
            ProtoHelper.Instance.Register<DeleteDBCharacterRequest>((int)DBCharacterProtocl.DeleteDbcharacterReq);
            ProtoHelper.Instance.Register<DeleteDBCharacterResponse>((int)DBCharacterProtocl.DeleteDbcharacterResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetDBCharacterRequest>(_HandleGetDBCharacterRequest);
            MessageRouter.Instance.Subscribe<AddDBCharacterRequset>(_HandleAddDBCharacterRequset);
            MessageRouter.Instance.Subscribe<DeleteDBCharacterRequest>(_HandleDeleteDBCharacterRequest);
        }

        public async void _HandleGetDBCharacterRequest(Connection sender, GetDBCharacterRequest message)
        {
            GetDBCharacterReponse resp = new();
            DBCharacterNode cNode = await CharacterOperations.Instance.GetCharacterByCidAsync(message.CId);
            if (cNode == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = $"No information found for the cId {message.CId}.";
                goto End;
            }
            else
            {
                resp.ChrNode = cNode;
                resp.ResultCode = 0;
            }

        End:
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
        private async void _HandleDeleteDBCharacterRequest(Connection sender, DeleteDBCharacterRequest message)
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

    }
}
