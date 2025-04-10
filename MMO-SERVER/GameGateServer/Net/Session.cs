using Common.Summer.Core;
using Google.Protobuf;
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
        public string m_uId;

        public string m_cId;
        public int curSceneId;

        public Connection Conn;                                       
        public float LastHeartTime;        // 用myTime                
        private ConcurrentQueue<IMessage> msgBuffer = new ConcurrentQueue<IMessage>();

        public Session(string sessionId, string Uid)
        {
            Id = sessionId;
            m_uId = Uid;
            LastHeartTime = Scheduler.UnixTime;
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
        public void Send(ByteString message)
        {
            if(Conn != null)
            {
                Conn.Send(message);
            }
        }

    }
}
