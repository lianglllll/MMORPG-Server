using System;
using System.Net.Sockets;
using Common.Summer.Net;
using Common.Summer.Proto;
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
        private LengthFieldDecoder m_lfd;                   //消息接受器
        private DisconnectedHandler m_onDisconnected;       //关闭连接的委托
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

            //给这个客户端连接创建一个解码器
            m_lfd = new LengthFieldDecoder(socket, 64 * 1024, 0, 4, 0, 4, _OnDataRecived, _OnDisconnected);
            m_lfd.Init();//启动解码器，开始接收消息

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
        private void _OnDataRecived(byte[] data)
        {
            ushort code = _GetUShort(data, 0);
            var msg = ProtoHelper.ParseFrom((int)code, data, 2, data.Length - 2);
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

        #region 发送网络数据包

        /// <summary>
        /// 发送消息包，编码过程(通用)
        /// </summary>
        /// <param name="message"></param>
        public void Send(Google.Protobuf.IMessage message)
        {
            try
            {
                //获取imessage类型所对应的编号，网络传输我们只传输编号
                using (var ds = DataStream.Allocate())
                {
                    int code = ProtoHelper.Type2Seq(message.GetType());
                    if(code == -1)
                    {
                        return;
                    }
                    ds.WriteInt(message.CalculateSize() + 2);             //长度字段
                    ds.WriteUShort((ushort)code);                       //协议编号字段
                    message.WriteTo(ds);                                //数据
                    _SocketSend(ds.ToArray());
                }
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }

        }
        public void Send(ByteString data)
        {
            _SocketSend(data.ToByteArray());
        }

        /// <summary>
        /// 通过socket发送，原生数据
        /// </summary>
        /// <param name="data"></param>
        private void _SocketSend(byte[] data)
        {
            _SocketSend(data, 0, data.Length);
        }

        /// <summary>
        /// 开始异步发送消息,原生数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        private void _SocketSend(byte[] data, int start, int len)
        {
            lock (this)//多线程问题，防止争夺send
            {
                if (m_socket!=null && m_socket.Connected)
                {
                    m_socket.BeginSend(data, start, len, SocketFlags.None, new AsyncCallback(_SendCallback), m_socket);
                }
            }
        }

        /// <summary>
        /// 异步发送消息回调
        /// </summary>
        /// <param name="ar"></param>
        private void _SendCallback(IAsyncResult ar)
        {
            if (m_socket != null && m_socket.Connected)
            {
                // 发送的字节数
                int len = m_socket.EndSend(ar);
            }

        }

        #endregion

        // 获取两个字节
        private ushort _GetUShort(byte[] data, int offset)
        {
            if (BitConverter.IsLittleEndian)
                return (ushort)((data[offset] << 8) | data[offset + 1]);
            return (ushort)((data[offset + 1] << 8) | data[offset]);
        }


    }
}
