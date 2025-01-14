using System.Collections.Concurrent;
using Common.Summer.Tools;
using Common.Summer.Core;
using HS.Protobuf.Login;
using Common.Summer.Net;
using System;
using HS.Protobuf.Game;

namespace GameServer.Net
{
    public class GameTokenManager : Singleton<GameTokenManager>
    {
        private ConcurrentDictionary<string, GameToken> m_tokens = new ConcurrentDictionary<string, GameToken>();

        public void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<GetGameTokenResponse>((int)GameProtocl.GetGameTokenResp);
        }

        public GameToken NewToken(Connection connection)
        {
            var token = new GameToken(Guid.NewGuid().ToString(), connection);
            m_tokens[token.Id] = token;

            GetGameTokenResponse resp = new();
            resp.GameToken = token.Id;
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
        public GameToken GetToken(string tokenId)
        {
            if (m_tokens.TryGetValue(tokenId, out var token))
            {
                return token;
            }
            return null;
        }

    }
}
