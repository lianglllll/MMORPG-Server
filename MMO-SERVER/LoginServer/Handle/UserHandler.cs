using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
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
            ProtoHelper.Register<UserLoginRequest>((int)LoginProtocl.UserLoginRequest);
            ProtoHelper.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResponse);
            ProtoHelper.Register<UserRegisterRequest>((int)LoginProtocl.UserRegisterRequest);
            ProtoHelper.Register<UserRegisterResponse>((int)LoginProtocl.UserRegisterResponse);
            ProtoHelper.Register<GetDBUserRequest>((int)DBUserProtocl.GetDbuserReq);
            ProtoHelper.Register<GetDBUserResponse>((int)DBUserProtocl.GetDbuserResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<UserLoginRequest>(_HandleUserLoginRequest);
            MessageRouter.Instance.Subscribe<UserRegisterRequest>(_HandleUserRegisterRequest);
            MessageRouter.Instance.Subscribe<GetDBUserResponse>(_HandleGetDBUserResponse);

            return true;
        }

        public bool UnInit()
        {
            return true;
        }

        private void _HandleUserLoginRequest(Connection conn, UserLoginRequest message)
        {
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

            UserLoginRequest userLoginRequest = (UserLoginRequest)m_tasks[message.TaskId];
            UserLoginResponse resp = new();
            resp.LoginGateToken = userLoginRequest.LoginGateToken;
            Connection gateConn = LoginTokenManager.Instance.GetToken(userLoginRequest.LoginToken).Conn;
            if(message.ResultCode != 0)
            {
                resp.ResultCode = 1;
                resp.ResultMsg = "登录失败，用户名不存在";
                goto End1;
            }

            // 验证用户名密码
            // 这里的密码需要进行加密之后再进行比较
            if (userLoginRequest.Password != message.User.Password)
            {
                resp.ResultCode = 2;
                resp.ResultMsg = "登录失败，密码错误";
                goto End1;
            }

            // 1.要考虑到今天限制登录，2.这个号被封了  3.被关小黑屋了  4.没实名认证

            // 如果当前用户在线，将其踢出游戏，防止同一账号多次登录
            Session session = SessionManager.Instance.GetSessionByUid(message.User.UId);
            if(session != null)
            {
                // 踢出游戏
                SessionManager.Instance.RemoveSession(session.Id);
            }

            // 分配session
            session = SessionManager.Instance.NewSession();
            session.dbUser = message.User;

            // 响应
            resp.ResultCode = 0;
            resp.ResultMsg = "登录成功";
            resp.SessionId = session.Id;

            // 通知其他server这给session的存在



            // 清理资源
            m_tasks.Remove(message.TaskId);
            m_idGenerator.ReturnId(message.TaskId);

        End1:
            gateConn.Send(resp);
        End2:
            return;
        }

        private void _HandleUserRegisterRequest(Connection conn, UserRegisterRequest message)
        {
        }

    }
}
