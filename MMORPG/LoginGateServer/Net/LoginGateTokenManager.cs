using System.Collections.Concurrent;
using Common.Summer.Tools;
using Common.Summer.Core;
using HS.Protobuf.Login;
using HS.Protobuf.LoginGate;
using Common.Summer.Net;

namespace LoginGateServer.Net
{
    public class LoginGateTokenManager : Singleton<LoginGateTokenManager>
    {
        private ConcurrentDictionary<string, LoginGateToken> m_tokens = new ConcurrentDictionary<string, LoginGateToken>();

        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetLoginGateTokenResponse>((int)LoginGateProtocl.GetLogingateTokenResp);

        }

        public LoginGateToken NewToken(Connection connection)
        {
            var token = new LoginGateToken(Guid.NewGuid().ToString(), connection);
            m_tokens[token.Id] = token;

            GetLoginGateTokenResponse resp = new GetLoginGateTokenResponse();
            resp.LoginGateToken = token.Id;
            connection.Send(resp);

            return token;
        }
        public LoginGateToken GetToken(string tokenId)
        {
            if (m_tokens.TryGetValue(tokenId, out var token))
            {
                return token;
            }
            return null;
        }
        public void RemoveToken(string tokenId)
        {
            m_tokens.TryRemove(tokenId, out var token);
            if (token != null)
            {
                token.Conn = null;
            }
        }
    }
}
