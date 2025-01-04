using System.Collections.Concurrent;
using Common.Summer.Tools;
using Common.Summer.Core;
using LoginServer.Net;
using HS.Protobuf.Common;
using Common.Summer.Proto;
using HS.Protobuf.Login;

namespace LoginGateServer.Net
{
    public class LoginTokenManager : Singleton<LoginTokenManager>
    {
        private ConcurrentDictionary<string, LoginToken> m_tokens = new ConcurrentDictionary<string, LoginToken>();

        public void Init()
        {
            // 协议注册
            ProtoHelper.Register<GetLoginTokenResponse>((int)LoginProtocl.GetLoginTokenResp);
        }

        public LoginToken NewToken(Connection connection)
        {
            var token = new LoginToken(Guid.NewGuid().ToString(), connection);
            m_tokens[token.Id] = token;

            GetLoginTokenResponse resp = new GetLoginTokenResponse();
            resp.LoginToken = token.Id;
            connection.Send(resp);

            return token;
        }
        public void RemoveToken(string tokenId)
        {
            m_tokens.TryRemove(tokenId, out var token);
            if (token != null)
            {
                token.Conn = null;
            }
        }
        public LoginToken GetToken(string tokenId)
        {
            if (m_tokens.TryGetValue(tokenId, out var token))
            {
                return token;
            }
            return null;
        }

    }
}
