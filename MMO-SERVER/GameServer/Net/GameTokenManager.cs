using System.Collections.Concurrent;
using Common.Summer.Tools;
using Common.Summer.Core;
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
        }

        public GameToken NewToken(Connection connection, int serverId)
        {
            var token = new GameToken(Guid.NewGuid().ToString(), connection, serverId);
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
