using Google.Protobuf;
using Common.Summer.Core;

namespace GameServer.Net
{
    public class GameToken
    {
        public string Id { get; private set; }
        public Connection Conn;                                             //网络连接对象
        public float LastHeartTime { get; set; }                            //心跳时间

        public GameToken(string sessionId, Connection connection)
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
