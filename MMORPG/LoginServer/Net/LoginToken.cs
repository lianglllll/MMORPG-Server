using Google.Protobuf;
using Common.Summer.Core;
using HS.Protobuf.Common;

namespace LoginGateServer.Net
{
    public class LoginToken
    {
        public string Id { get; private set; }
        public Connection Conn;                                             //网络连接对象
        public float LastHeartTime { get; set; }                            //心跳时间
        public ServerInfoNode ServerInfoNode { get; set; }

        public LoginToken(string sessionId, Connection connection , ServerInfoNode serverInfoNode)
        {
            Id = sessionId;
            Conn = connection;
            ServerInfoNode =  serverInfoNode;
            LastHeartTime = MyTime.time;
        }

        public void Send(IMessage message)
        {
            Conn?.Send(message);
        }
    }
}
