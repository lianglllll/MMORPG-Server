using GameServer.core;
using GameServer.Database;
using GameServer.Network;
using GameServer;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Timers;
using Common.Summer.GameServer;

namespace GameServer.Manager
{
    public class SessionManager:Singleton<SessionManager>
    {
        private int SESSIONTIMEOUT = 120;

        //session字典<sessionid,session>
        private ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public int OnlineUserCount => sessions.Count;

        public SessionManager()
        {
            //创建一个计时器,1秒触发
            var timer = new Timer(1000);
            timer.Elapsed += (sender, e) => CheckSession();
            timer.Start();
        }

        /// <summary>
        /// 分配session
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="session"></param>
        public Session NewSession(DbUser dbUser)
        {
            var session = new Session(Guid.NewGuid().ToString());
            session.dbUser = dbUser;
            sessions[session.Id] = session;
            return session;
        }

        public Session GetSession(string sessionId)
        {
            if(sessions.TryGetValue(sessionId, out var session))
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
                //让其conn连接失效
                NetService.Instance.ActiveClose(session.Conn);
                session.Conn = null;
            }
        }

        /// <summary>
        /// 通过用户id查找session
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Session GetSessionByUserId(int userId)
        {
            return sessions.Values.FirstOrDefault(s => s.dbUser.Id == userId);
        }

        /// <summary>
        /// 检测过期的session，并且清理
        /// </summary>
        public void CheckSession()
        {
            sessions.Values.AsParallel()
                .Where(s => (MyTime.time - s.LastHeartTime) > SESSIONTIMEOUT)
                .ForAll(s => s.Leave());
        }

    }
}
