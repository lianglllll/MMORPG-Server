using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Login;
using LoginGateServer.Net;

namespace LoginGateServer.Handle
{
    public class UserHandler : Singleton<UserHandler>
    {
        public bool Init()
        {
            // 协议注册
            ProtoHelper.Register<UserLoginRequest>((int)LoginProtocl.UserLoginRequest);
            ProtoHelper.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResponse);
            ProtoHelper.Register<UserRegisterRequest>((int)LoginProtocl.UserRegisterRequest);
            ProtoHelper.Register<UserRegisterResponse>((int)LoginProtocl.UserRegisterResponse);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<UserLoginRequest>(_HandleUserLoginRequest);
            MessageRouter.Instance.Subscribe<UserLoginResponse>(_HandleUserLoginResponse);
            MessageRouter.Instance.Subscribe<UserRegisterRequest>(_HandleUserRegisterRequest);
            MessageRouter.Instance.Subscribe<UserRegisterResponse>(_HandleUserRegisterResponse);
            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        private void _HandleUserLoginRequest(Connection conn, UserLoginRequest message)
        {
            // 解密

            // 转发到loginServer
            message.LoginToken = ServersMgr.Instance.LoginToken;
            ServersMgr.Instance.SentToLoginServer(message);
        }
        private void _HandleUserLoginResponse(Connection conn, UserLoginResponse message)
        {
            LoginGateToken token = LoginGateTokenManager.Instance.GetToken(message.LoginGateToken);
            message.LoginGateToken = "";
            token.Send(message);
        }
        private void _HandleUserRegisterRequest(Connection conn, UserRegisterRequest message)
        {
            // 解密

            // 转发到loginServer
            ServersMgr.Instance.SentToLoginServer(message);
        }
        private void _HandleUserRegisterResponse(Connection conn, UserRegisterResponse message)
        {
            LoginGateToken token = LoginGateTokenManager.Instance.GetToken(message.LoginGateToken);
            token.Send(message);
        }
    }
}
