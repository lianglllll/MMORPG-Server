using GameServer.Database;
using GameServer.Manager;
using GameServer.Model;
using Google.Protobuf;
using Serilog;
using GameServer.Network;
using System.Collections.Concurrent;
using Common.Summer.GameServer;

namespace GameServer.Net
{
    /// <summary>
    /// 用户会话,代表玩家客户端，只有登录成功的用户才有session
    /// </summary>
    public class Session
    {
        public string Id { get; private set; }
        public DbUser dbUser;                                               //用户信息
        public Character character;                                         //当前用户使用的角色
        public Connection Conn;                                             //网络连接对象

        //如果网络连接断开，就把消息临时缓存在这里
        private ConcurrentQueue<IMessage> msgBuffer = new ConcurrentQueue<IMessage>();

        public Space Space => character?.currentSpace;                      //当前所在地图

        public float LastHeartTime { get; set; }                            //心跳时间


        private Session()
        {

        }

        public Session(string sessionId)
        {
            Id = sessionId;
            LastHeartTime = MyTime.time;
        }

        /// <summary>
        /// 向客户端发送消息
        /// </summary>
        /// <param name="message"></param>
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

        /// <summary>
        /// session的离开游戏
        /// </summary>
        public void Leave()
        {
            Log.Information("session过期");
            //让session失效
            SessionManager.Instance.RemoveSession(Id);
            //移除chr
            if (character != null)
            {
                character.currentSpace?.EntityLeave(character);
                CharacterManager.Instance.RemoveCharacter(character.EntityId);
            }
        }
    }
}
