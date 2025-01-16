using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Net;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.Game;
using System.Collections.Generic;

namespace GameServer.Handle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();

        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetCharacterListRequest>((int)GameProtocl.GetCharacterListReq);
            ProtoHelper.Instance.Register<GetCharacterListResponse>((int)GameProtocl.GetCharacterListResp);
            ProtoHelper.Instance.Register<CreateCharacterRequest>((int)GameProtocl.CreateCharacterReq);
            ProtoHelper.Instance.Register<CreateCharacterResponse>((int)GameProtocl.CreateCharacterResp);
            ProtoHelper.Instance.Register<DeleteCharacterRequest>((int)GameProtocl.DeleteCharacterReq);
            ProtoHelper.Instance.Register<DeleteCharacterResponse>((int)GameProtocl.DeleteCharacterResp);
            ProtoHelper.Instance.Register<EnterGameRequest>((int)GameProtocl.EnterGameReq);
            ProtoHelper.Instance.Register<EnterGameResponse>((int)GameProtocl.EnterGameResp);
            ProtoHelper.Instance.Register<GetDBCharactersByUidRequest>((int)DBCharacterProtocl.GetDbcharactersByUidReq);
            ProtoHelper.Instance.Register<GetDBCharactersByUidResponse>((int)DBCharacterProtocl.GetDbcharactersByUidResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetCharacterListRequest>(_HandleGetCharacterListRequest);
            MessageRouter.Instance.Subscribe<GetDBCharactersByUidResponse>(_HandleGetDBCharactersByUidResponse);

            MessageRouter.Instance.Subscribe<CreateCharacterRequest>(_HandleCreateCharacterRequest);
            MessageRouter.Instance.Subscribe<DeleteCharacterRequest>(_HandleDeleteCharacterRequest);
            MessageRouter.Instance.Subscribe<EnterGameRequest>(_HandleEnterGameRequest);

            return true;
        }

        private void _HandleGetCharacterListRequest(Connection conn, GetCharacterListRequest message)
        {
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            // 查询数据库
            GetDBCharactersByUidRequest req = new();
            req.TaskId = taskId;
            req.UId = message.UId;
            ServersMgr.Instance.SendMsgToDBProxy(req);
        }
        private void _HandleGetDBCharactersByUidResponse(Connection conn, GetDBCharactersByUidResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }
            GetCharacterListRequest req = (GetCharacterListRequest)m_tasks[message.TaskId];
            GetCharacterListResponse resp = new();
            resp.SessionId = req.SessionId;
            var gateConn = GameTokenManager.Instance.GetToken(req.GameToken).Conn;

            resp.ResultCode = message.ResultCode;
            if (message.ResultCode != 0)
            {
                resp.ResultMsg = message.ResultMsg;
                goto End1;
            }

            foreach (var node in message.CNodes)
            {
                SimpleCharacterInfoNode infoNode = new();
                infoNode.CId = node.CId;
                infoNode.ChrName = node.ChrName;
                infoNode.ProfessionId = node.ProfessionId;
                infoNode.Level = node.Level;
                resp.CharacterNodes.Add(infoNode);
            }

            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);

        End1:
            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleCreateCharacterRequest(Connection conn, CreateCharacterRequest message)
        {
        }

        private void _HandleDeleteCharacterRequest(Connection conn, DeleteCharacterRequest message)
        {
        }

        private void _HandleEnterGameRequest(Connection conn, EnterGameRequest message)
        {
        }
    }
}
