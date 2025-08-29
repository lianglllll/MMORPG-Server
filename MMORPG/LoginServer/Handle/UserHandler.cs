using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Security;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBUser;
using HS.Protobuf.Login;
using LoginGateServer.Net;
using LoginServer.Net;

namespace LoginServer.Handle
{
    public class UserHandler : Singleton<UserHandler>
    {
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();


        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<UserLoginRequest>((int)LoginProtocl.UserLoginReq);
            ProtoHelper.Instance.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResp);
            ProtoHelper.Instance.Register<UserRegisterRequest>((int)LoginProtocl.UserRegisterReq);
            ProtoHelper.Instance.Register<UserRegisterResponse>((int)LoginProtocl.UserRegisterResp);
            ProtoHelper.Instance.Register<GetDBUserRequest>((int)DBUserProtocl.GetDbuserReq);
            ProtoHelper.Instance.Register<GetDBUserResponse>((int)DBUserProtocl.GetDbuserResp);
            ProtoHelper.Instance.Register<AddDBUserRequset>((int)DBUserProtocl.AddDbuserReq);
            ProtoHelper.Instance.Register<AddDBUserResponse>((int)DBUserProtocl.AddDbuserResp);
            ProtoHelper.Instance.Register<VerifyUserNameExistenceRequest>((int)DBUserProtocl.VerifyDbuserNameExistenceReq);
            ProtoHelper.Instance.Register<VerifyUserNameExistenceResponse>((int)DBUserProtocl.VerifyDbuserNameExistenceResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<UserLoginRequest>(_HandleUserLoginRequest);
            MessageRouter.Instance.Subscribe<UserRegisterRequest>(_HandleUserRegisterRequest);
            MessageRouter.Instance.Subscribe<GetDBUserResponse>(_HandleGetDBUserResponse);
            MessageRouter.Instance.Subscribe<AddDBUserResponse>(_HandleAddDBUserResponse);

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        private void _HandleUserLoginRequest(Connection conn, UserLoginRequest message)
        {
            // 先找缓存

            //查询数据库
            GetDBUserRequest getDBUserRequest = new GetDBUserRequest();
            getDBUserRequest.UserName = message.Username;
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            getDBUserRequest.TaskId = taskId;
            ServersMgr.Instance.SendMsgToDBProxy(getDBUserRequest);
        }
        private void _HandleGetDBUserResponse(Connection sender, GetDBUserResponse message)
        {
            if(!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            UserLoginRequest req = (UserLoginRequest)m_tasks[message.TaskId];
            UserLoginResponse resp = new();
            resp.LoginGateToken = req.LoginGateToken;
            Connection gateConn = LoginTokenManager.Instance.GetToken(req.LoginToken).Conn;
            if(message.ResultCode == 1)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "登录失败，用户名不存在";
                goto End1;
            }

            DBUserNode dBUserNode = message.User;
            // 验证用户名密码
            if (PasswordHasher.Instance.VerifyPassword(req.Password, dBUserNode.Password) == false)
            {
                resp.ResultCode = 2;
                resp.ResultMsg = "登录失败，密码错误";
                goto End1;
            }

            // 1.要考虑到今天限制登录，2.被关小黑屋了  3.没实名认证
            if(dBUserNode.AccountStatus == "suspended")
            {
                resp.ResultCode = 3;
                resp.ResultMsg = $"登录失败，当前账号正在小黑屋，预计解封时间{dBUserNode.LockedUntilTimesTamp}";
                goto End1;
            }

            // 如果当前用户在线，将其踢出游戏，防止同一账号多次登录
            Session session = SessionManager.Instance.GetSessionByUId(dBUserNode.UId);
            if(session != null)
            {
                SessionManager.Instance.RemoveSession(session.Id);
                // 如果当前session已经连接到GateGate，通知当前这个session对应的Gate，让其t玩家下线。
                // todo...


            }

            // 分配session
            session = SessionManager.Instance.NewSession(dBUserNode.UId);
            session.dbUser = dBUserNode;

            // 响应
            resp.ResultCode = 0;
            resp.ResultMsg = "登录成功";
            resp.SessionId = session.Id;

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);

            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleUserRegisterRequest(Connection conn, UserRegisterRequest message)
        {
            UserRegisterResponse resp = new();
            resp.LoginGateToken = message.LoginGateToken;
            
            // 名字长度&&敏感词验证
            if(message.Username == null)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "注册失败，用户名不能为空。";
                goto End;
            }
            if (message.Username.Length < 2)
            {
                resp.ResultCode = 2;
                resp.ResultMsg = "注册失败，用户名长度不能短于2。";
                goto End;
            }
            if (message.Username.Length > 24)
            {
                resp.ResultCode = 3;
                resp.ResultMsg = "注册失败，用户名长度不能长于24。";
                goto End;
            }

            // 密码长度验证
            if (message.Password == null)
            {
                resp.ResultCode = 4;
                resp.ResultMsg = "注册失败，密码不能为空。";
                goto End;
            }
            if (message.Password.Length < 6)
            {
                resp.ResultCode = 5;
                resp.ResultMsg = "注册失败，密码长度不能短于6。";
                goto End;
            }
            if (message.Password.Length > 36)
            {
                resp.ResultCode = 6;
                resp.ResultMsg = "注册失败，密码长度不能长于36。";
                goto End;
            }

            // 密码加密
            string hashPassword = PasswordHasher.Instance.HashPassword(message.Password);

            // 验证用户名是否重复
            AddDBUserRequset addDBUserRequset = new();
            DBUserNode userNode = new();
            addDBUserRequset.DbUserNode = userNode;
            userNode.UserName = message.Username;
            userNode.Password = hashPassword; 
            int taskId = m_idGenerator.GetId();
            m_tasks.Add(taskId, message);
            addDBUserRequset.TaskId = taskId;
            ServersMgr.Instance.SendMsgToDBProxy(addDBUserRequset);
            goto End1;
        End:
            conn.Send(message);
        End1:
            return;
        }
        private void _HandleAddDBUserResponse(Connection sender, AddDBUserResponse message)
        {
            if (!m_tasks.ContainsKey(message.TaskId))
            {
                goto End2;
            }

            UserRegisterRequest userRigisterRequest = (UserRegisterRequest)m_tasks[message.TaskId];
            UserRegisterResponse resp = new();
            resp.LoginGateToken = userRigisterRequest.LoginGateToken;
            Connection gateConn = LoginTokenManager.Instance.GetToken(userRigisterRequest.LoginToken).Conn;

            if (message.ResultCode == 1)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "当前用户名已被注册。";
                goto End1;
            }
            else if (message.ResultCode == 2) {
                resp.ResultCode = 2;
                resp.ResultMsg = "注册失败，未知原因，请联系管理员。";
                goto End1;
            } else if (message.ResultCode == 0)
            {
                resp.ResultCode = 0;
                resp.ResultMsg = "注册成功。";
            }

        End1:
            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);
            gateConn.Send(resp);
        End2:
            return;
        }
    }
}
