using Google.Protobuf;
using Common.Summer.Core;

namespace SceneServer.Net
{
    /// <summary>
    /// 用户会话,代表玩家客户端，
    /// </summary>
    public class Session
    {
        private string m_sessionId; 
        private Connection m_conn;                                             // 对应的网关连接
        public string SesssionId => m_sessionId;
        public Session(string sessionId, Connection conn)
        {
            m_sessionId = sessionId;
            m_conn = conn;
        }
        public void Send(IMessage message)
        {
            m_conn?.Send(message);
        }
    }
}
