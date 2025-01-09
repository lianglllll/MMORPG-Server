using System.Collections.Concurrent;
using Common.Summer.Tools;

namespace LoginServer.Net
{
    public class SessionManager : Singleton<SessionManager>
    {
        //session字典<sessionid,session>
        private ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public void Init()
        {
        }
        public Session NewSession()
        {
            var session = new Session(Guid.NewGuid().ToString());
            sessions[session.Id] = session;
            return session;
        }
        public Session GetSessionByUid(string sessionId)
        {
            if (sessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }
        public Session GetSession(string uID)
        {

            return null;
        }
        public void RemoveSession(string sessionId)
        {
            sessions.TryRemove(sessionId, out var session);
        }
    }
}
