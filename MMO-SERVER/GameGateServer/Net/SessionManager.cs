using System.Collections.Concurrent;
using Common.Summer.Core;
using Common.Summer.Tools;

namespace GameGateServer.Net
{
    public class SessionManager : Singleton<SessionManager>
    {
        //session字典<sessionid,session>
        private ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();
        public void Init()
        {
        }
        public Session NewSession(string sessionId)
        {
            var session = new Session(sessionId);
            sessions[sessionId] = session;
            return session;
        }
        public Session GetSessionBySessionId(string sessionId)
        {
            if (sessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }
        public void RemoveSession(string sessionId)
        {
            sessions.TryRemove(sessionId, out var session);
        }
    }
}
