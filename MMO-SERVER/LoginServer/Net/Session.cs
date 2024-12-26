using Common.Summer.Core;

namespace LoginServer.Net
{
    /// <summary>
    /// 用户会话,代表玩家客户端，只有登录成功的用户才分配给他session
    /// </summary>
    public class Session
    {
        public string Id { get; private set; }
        public Connection Conn;                                             //网络连接对象

        public float LastHeartTime { get; set; }                            //心跳时间

        public Session(string sessionId)
        {
            Id = sessionId;
            LastHeartTime = MyTime.time;
        }

    }
}
