using System.Collections.Concurrent;
using Common.Summer.Tools;

namespace LoginServer.Net
{
    public class SessionManager : Singleton<SessionManager>
    {
        //session字典<sessionid,session>
        private ConcurrentDictionary<string, Session> sessions1 = new ConcurrentDictionary<string, Session>();
        // <uid, session>
        private ConcurrentDictionary<string, Session> sessions2 = new ConcurrentDictionary<string, Session>();

        public void Init()
        {
        }
        public Session NewSession(string uId)
        {
            var session = new Session(Guid.NewGuid().ToString());
            sessions1[session.Id] = session;
            sessions2[uId] = session;
            return session;
        }
        public Session GetSessionBySessionId(string sessionId)
        {
            if (sessions1.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }
        public Session GetSessionByUId(string uId)
        {
            if (sessions2.TryGetValue(uId, out var session))
            {
                return session;
            }
            return null;
        }

        public void RemoveSession(string sessionId)
        {
            sessions1.TryRemove(sessionId, out var session);
            if(session != null)
            {
                sessions2.TryRemove(session.dbUser.UId, out _);
            }
        }
    }
}
