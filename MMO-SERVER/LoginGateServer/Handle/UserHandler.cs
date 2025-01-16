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
            ProtoHelper.Instance.Register<UserLoginRequest>((int)LoginProtocl.UserLoginReq);
            ProtoHelper.Instance.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResp);
            ProtoHelper.Instance.Register<UserRegisterRequest>((int)LoginProtocl.UserRegisterReq);
            ProtoHelper.Instance.Register<UserRegisterResponse>((int)LoginProtocl.UserRegisterResp);
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
            message.Username = conn.m_encryptionManager.AesDecrypt(message.Username);
            message.Password = conn.m_encryptionManager.AesDecrypt(message.Password);
            // 转发到loginServer
            message.LoginToken = ServersMgr.Instance.LoginToken;
            ServersMgr.Instance.SendToLoginServer(message);
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
            message.Username = conn.m_encryptionManager.AesDecrypt(message.Username);
            message.Password = conn.m_encryptionManager.AesDecrypt(message.Password);
            message.LoginToken = ServersMgr.Instance.LoginToken;

            // 转发到loginServer
            ServersMgr.Instance.SendToLoginServer(message);
        }
        private void _HandleUserRegisterResponse(Connection conn, UserRegisterResponse message)
        {
            LoginGateToken token = LoginGateTokenManager.Instance.GetToken(message.LoginGateToken);
            message.LoginGateToken = "";
            token.Send(message);
        }
    }
}
