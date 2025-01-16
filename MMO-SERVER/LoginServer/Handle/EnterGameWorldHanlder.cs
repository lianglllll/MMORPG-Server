using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Security;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Common;
using HS.Protobuf.DBProxy.DBUser;
using HS.Protobuf.DBProxy.DBWorld;
using HS.Protobuf.GameGateMgr;
using HS.Protobuf.Login;
using LoginGateServer.Net;
using LoginServer.Net;

namespace LoginServer.Handle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();

        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetAllWorldInfosRequest>((int)LoginProtocl.GetAllWorldInfoNodeReq);
            ProtoHelper.Instance.Register<GetAllWorldInfosResponse>((int)LoginProtocl.GetAllWorldInfoNodeResp);
            ProtoHelper.Instance.Register<GetAllDBWorldNodeRequest>((int)DBWorldProtocl.GetAllDbworldNodeReq);
            ProtoHelper.Instance.Register<GetAllDBWorldNodeResponse>((int)DBWorldProtocl.GetAllDbworldNodeResp);
            ProtoHelper.Instance.Register<GetGameGateByWorldIdRequest>((int)LoginProtocl.GetGameGateByWorldidReq);
            ProtoHelper.Instance.Register<GetGameGateByWorldIdResponse>((int)LoginProtocl.GetGameGateByWorldidResp);
            ProtoHelper.Instance.Register<RegisterSessionToGGMRequest>((int)GameGateMgrProtocl.RegisterSessoionToGgmReq);
            ProtoHelper.Instance.Register<RegisterSessionToGGMResponse>((int)GameGateMgrProtocl.RegisterSessoionToGgmResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<GetAllWorldInfosRequest>(_HandleGetAllWorldInfosRequest);
            MessageRouter.Instance.Subscribe<GetAllDBWorldNodeResponse>(_HandleGetAllDBWorldNodeResponse);
            MessageRouter.Instance.Subscribe<GetGameGateByWorldIdRequest>(_HandleGetGameGateByWorldIdRequest);
            MessageRouter.Instance.Subscribe<RegisterSessionToGGMResponse>(_HandleRegisterSessionToGGMResponse);

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        private void _HandleGetAllWorldInfosRequest(Connection conn, GetAllWorldInfosRequest message)
        {
            //查询数据库
            GetAllDBWorldNodeRequest req = new();
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            req.TaskId = taskId;
            ServersMgr.Instance.SendMsgToDBProxy(req);
        }
        private void _HandleGetAllDBWorldNodeResponse(Connection conn, GetAllDBWorldNodeResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            GetAllWorldInfosRequest req = (GetAllWorldInfosRequest)m_tasks[message.TaskId];
            GetAllWorldInfosResponse resp = new();
            resp.LoginGateToken = req.LoginGateToken;
            Connection gateConn = LoginTokenManager.Instance.GetToken(req.LoginToken).Conn;
            if (message.ResultCode == 1)
            {
                goto End1;
            }

            foreach(var wNode in message.Nodes)
            {
                WorldInfoNode node = new();
                node.WorldId = wNode.WorldId;
                node.WorldName = wNode.WorldName;
                node.WorldDesc = wNode.WorldDesc;
                if(wNode.Status == "active")
                {
                    node.Status = WORLD_LOAD_STATUS.Idle;
                }else if(wNode.Status == "inActive")
                {
                    node.Status = WORLD_LOAD_STATUS.Offline;
                }
                resp.WorldInfoNodes.Add(node);
            }

        End1:
            gateConn.Send(resp);
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
        End2:
            return;
        }
        
        private void _HandleGetGameGateByWorldIdRequest(Connection conn, GetGameGateByWorldIdRequest message)
        {
            // 校验一下有没有这个session
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            GetGameGateByWorldIdResponse resp = new();
            if(session == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "越权操作,请登录!";
                resp.LoginGateToken = message.LoginGateToken;
                goto End1;
            }

            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            RegisterSessionToGGMRequest req = new();
            req.TaskId = taskId;
            req.WorldId = message.WorldId;
            req.SessionId = message.SessionId;
            req.UId = session.dbUser.UId;
            ServersMgr.Instance.SendMsgToGGM(req);
            goto End2;

        End1:
            conn.Send(resp);
            return;
        End2:
            return;
        }
        private void _HandleRegisterSessionToGGMResponse(Connection conn, RegisterSessionToGGMResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            GetGameGateByWorldIdRequest req = (GetGameGateByWorldIdRequest)m_tasks[message.TaskId];
            GetGameGateByWorldIdResponse resp = new();
            resp.LoginGateToken = req.LoginGateToken;
            Connection gateConn = LoginTokenManager.Instance.GetToken(req.LoginToken).Conn;
            if (message.ResultCode != 0)
            {
                resp.ResultCode = message.ResultCode;
                resp.ResultMsg = message.ResultMsg;
                goto End1;
            }

            // 处理.....
            resp.ResultCode = 0;
            foreach(var item in message.GameGateInfos)
            {
                ServerInfoNode node = new();
                node.Ip = item.Ip;
                node.Port = item.GameGateServerInfo.UserPort;
                resp.GameGateInfos.Add(node);
            }

        End1:
            gateConn.Send(resp);
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
        End2:
            return;
        }
    }
}
