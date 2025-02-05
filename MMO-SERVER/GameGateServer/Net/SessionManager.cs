using System.Collections.Concurrent;
using System.Diagnostics;
using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.GameGate;
using Serilog;

namespace GameGateServer.Net
{
    public class SessionManager : Singleton<SessionManager>
    {
        private int SESSIONTIMEOUT = 120;
        //session字典<sessionid,session>
        private ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();
        
        public void Init()
        {
            Scheduler.Instance.AddTask(_CheckSession, 1000, 0);
        }

        public Session NewSession(RegisterSessionToGGRequest message)
        {
            var session = new Session(message.SessionId, message.UId);
            sessions[message.SessionId] = session;
            return session;
        }
        public void RemoveSessionById(string sessionId)
        {
            sessions.TryRemove(sessionId, out var session);
        }
        public Session GetSessionBySessionId(string sessionId)
        {
            if (sessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }
        private void _CheckSession()
        {
            foreach (var session in sessions.Values) { 
                if(MyTime.time - session.LastHeartTime > SESSIONTIMEOUT)
                {
                    Log.Debug("session超时");
                    ConnManager.Instance.CloseUserConnection(session.Conn);
                    RemoveSessionById(session.Id);
                }
            }
        }
    }
}
