using System.Collections.Concurrent;
using System.Timers;
using Common.Summer.Tools;
using Common.Summer.Core;

namespace LoginServer.Net
{
    public class SessionManager : Singleton<SessionManager>
    {
        private int SESSIONTIMEOUT = 120;
        //session字典<sessionid,session>
        private ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public int OnlineUserCount => sessions.Count;

        public void Init()
        {
            //创建一个计时器,1秒触发
            Scheduler.Instance.AddTask(CheckAndSweepSession, 1000, 0);
        }

        // 分配session
        public Session NewSession()
        {
            var session = new Session(Guid.NewGuid().ToString());
            sessions[session.Id] = session;
            return session;
        }

        public Session GetSession(string sessionId)
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
            if (session != null)
            {

            }
        }

        public Session GetSessionByUserId(int userId)
        {
            return null;
        }

        public void CheckAndSweepSession()
        {

        }

    }
}
