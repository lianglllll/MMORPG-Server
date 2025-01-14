using Common.Summer.Core;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBUser;
using Serilog;
using System.Collections.Concurrent;

namespace GameGateServer.Net
{
    /// <summary>
    /// 用户会话,代表玩家客户端，只有登录成功的用户才分配给他session
    /// </summary>
    public class Session
    {
        public string Id { get; private set; }
        public Connection Conn;                                             
        public float LastHeartTime;                        
        private ConcurrentQueue<IMessage> msgBuffer = new ConcurrentQueue<IMessage>();

        public Session(string sessionId)
        {
            Id = sessionId;
        }
        public void Send(IMessage message)
        {
            if (Conn != null)
            {
                while (msgBuffer.TryDequeue(out var msg))
                {
                    Log.Information("补发消息：" + msg);
                    Conn.Send(msg);
                }
                Conn.Send(message);
            }
            else
            {
                //说明当前角色离线了，我们将数据写入缓存中
                msgBuffer.Enqueue(message);
            }
        }
    }
}
