using System.Collections.Concurrent;
using Common.Summer.Tools;
using Common.Summer.Core;
using LoginServer.Net;
using HS.Protobuf.Common;
using HS.Protobuf.Login;
using Common.Summer.Net;

namespace LoginGateServer.Net
{
    public class LoginTokenManager : Singleton<LoginTokenManager>
    {
        private ConcurrentDictionary<string, LoginToken> m_tokens = new ConcurrentDictionary<string, LoginToken>();

        public void Init()
        {

        }

        public LoginToken NewToken(Connection connection, ServerInfoNode serverInfoNode)
        {
            var token = new LoginToken(Guid.NewGuid().ToString(), connection, serverInfoNode);
            m_tokens[token.Id] = token;
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
