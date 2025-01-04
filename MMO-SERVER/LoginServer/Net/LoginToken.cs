using Google.Protobuf;
using Common.Summer.Core;

namespace LoginGateServer.Net
{
    public class LoginToken
    {
        public string Id { get; private set; }
        public Connection Conn;                                             //网络连接对象
        public float LastHeartTime { get; set; }                            //心跳时间

        public LoginToken(string sessionId, Connection connection)
        {
            Id = sessionId;
            Conn = connection;
            LastHeartTime = MyTime.time;
        }

        public void Send(IMessage message)
        {
            Conn?.Send(message);
        }
    }
}
