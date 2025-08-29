using Google.Protobuf;
using Common.Summer.Core;

namespace GameServer.Net
{
    public class GameToken
    {
        public string Id { get; private set; }
        public Connection Conn;                                             //网络连接对象
        public float LastHeartTime { get; set; }                            //心跳时间
        public int ServerId { get; private set; }

        public GameToken(string sessionId, Connection connection, int serverId)
        {
            Id = sessionId;
            Conn = connection;
            ServerId = serverId;
            LastHeartTime = MyTime.time;
        }

        public void Send(IMessage message)
        {
            Conn?.Send(message);
        }
    }
}
