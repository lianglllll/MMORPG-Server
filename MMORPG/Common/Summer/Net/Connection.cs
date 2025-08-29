using System;
using System.Linq;
using System.Net.Sockets;
using Common.Summer.Net;
using Common.Summer.Security;
using Common.Summer.Tools;
using Google.Protobuf;
using Serilog;

namespace Common.Summer.Core
{
    /// <summary>
    /// 通用的网络连接，可以继承本类进行功能扩展
    /// 职责： 
    ///        1.接收、发送网络消息
    ///        2.处理网络断开的时候需要有一个事件通知(回调)
    ///        3.关闭连接(回调)
    /// </summary>
    public class Connection:TypeAttributeStore
    {
        public delegate void DisconnectedHandler(Connection sender);
        private Socket m_socket;                            // 连接客户端的socket
        private LengthFieldDecoder m_lfd;                   // 消息接受器
        private DisconnectedHandler m_onDisconnected;       // 关闭连接的委托
        public EncryptionManager m_encryptionManager;       // 安全相关
        public Socket Socket
        {
            get
            {
                return m_socket;
            }
        }

        public bool Init(Socket socket, DisconnectedHandler disconnected)
        {
            m_socket = socket;
            // m_socket.NoDelay = true;
            // 给这个客户端连接创建一个解码器
            m_lfd = new LengthFieldDecoder(socket, 64 * 1024, 0, 4,
                0, 4, _OnDataRecived, _OnDisconnected);
            _ = m_lfd.StartAsync();   // 启动解码器，开始接收消息

            m_onDisconnected = disconnected;

            // 加密模块
            m_encryptionManager = new EncryptionManager();
            m_encryptionManager.Init();

            return true;
        }

        private void _OnDisconnected()
        {
            m_socket = null;
            //向上转发，让其删除本connection对象
            m_onDisconnected?.Invoke(this);
        }
        private void _OnDataRecived(ReadOnlyMemory<byte> data)
        {
            var msg = ProtoHelper.Instance.ByteArrayParse2IMessage(data);
            if(msg == null)
            {
                return;
            }
            //交给消息路由，让其帮忙转发
            if (MessageRouter.Instance.Running)
            {
                MessageRouter.Instance.AddMessage(this, msg);
            }
        }
        public void CloseConnection()
        {
            m_socket = null;
            //转交给下一层的解码器关闭连接
            m_lfd.ActiveDisconnection();
        }

        public void Send(IMessage message)
        {
            try
            {
                _SocketSend(ProtoHelper.Instance.IMessageParse2ByteArray(message));
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }

        }
        public void Send(ByteString message)
        {
            try
            {
                _SocketSend(message.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
        private void _SocketSend(byte[] data)
        {
            if (data == null) return;
            lock (this)// 多线程问题，防止争夺send
            {
                if (m_socket != null && m_socket.Connected)
                {
                    m_socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(_SendCallback), m_socket);
                }
            }
        }
        private void _SendCallback(IAsyncResult ar)
        {
            if (m_socket != null && m_socket.Connected)
            {
                // 发送的字节数
                int len = m_socket.EndSend(ar);
            }

        }
    }
}
