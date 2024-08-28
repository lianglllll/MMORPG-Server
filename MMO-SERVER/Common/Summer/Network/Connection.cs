using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Serilog;
using GameServer.core;
using Proto;
using System.Threading;
using System.Collections;

namespace GameServer.Network
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
        //连接客户端的socket
        private Socket _socket;         
        public Socket Socket
        {
            get
            {
                return _socket;
            }
        }
        public LengthFieldDecoder lfd;

        public delegate void DataReceivedHandler(Connection sender,IMessage data);
        public delegate void DisconnectedHandler(Connection sender);
        public DataReceivedHandler OnDataReceived;//消息接收的委托  todo这玩意貌似没有用上，因为消息我们直接交给消息路由了
        public DisconnectedHandler OnDisconnected;//关闭连接的委托

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="socket"></param>
        public Connection(Socket socket)
        {
            this._socket = socket;

            //给这个客户端连接创建一个解码器
            lfd = new LengthFieldDecoder(socket, 64 * 1024, 0, 4, 0, 4);
            lfd.Received += OnDataRecived;
            lfd.disconnected += _OnDisconnected;
            lfd.Start();//启动解码器，开始接收消息

        }

        /// <summary>
        /// 断开连接回调
        /// </summary>
        private void _OnDisconnected()
        {
            _socket = null;
            //向上转发，让其删除本connection对象
            OnDisconnected?.Invoke(this);
        }

        //todo,我们调用底层解码器的关闭连接函数
        /// <summary>
        /// 主动关闭连接
        /// </summary>
        public void ActiveClose()
        {
            _socket = null;
            //转交给下一层的解码器关闭连接
            lfd.ActiveClose();
        }

        /// <summary>
        /// 接收到消息的回调
        /// </summary>
        /// <param name="data"></param>
        private void OnDataRecived(byte[] data)
        {
            ushort code = GetUShort(data, 0);
            var msg = ProtoHelper.ParseFrom((int)code, data, 2, data.Length-2);

            //Console.WriteLine(" 正在运行， 线程id：{0}，是否线程池线程: {1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);

            //交给消息路由，让其帮忙转发
            if (MessageRouter.Instance.Running)
            {
                MessageRouter.Instance.AddMessage(this,msg);
            }
        }

        /// <summary>
        /// 获取data数据，偏移offset。获取两个字节
        /// 前提：data必须是大端字节序
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private ushort GetUShort(byte[] data, int offset)
        {
            if (BitConverter.IsLittleEndian)
                return (ushort)((data[offset] << 8) | data[offset + 1]);
            return (ushort)((data[offset + 1] << 8) | data[offset]);
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
                    int code = ProtoHelper.SeqCode(message.GetType());
                    ds.WriteInt(message.CalculateSize() + 2);             //长度字段
                    ds.WriteUShort((ushort)code);                       //协议编号字段
                    message.WriteTo(ds);                                //数据
                    SocketSend(ds.ToArray());
                }
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }

        }

        /// <summary>
        /// 通过socket发送，原生数据
        /// </summary>
        /// <param name="data"></param>
        private void SocketSend(byte[] data)
        {
            SocketSend(data, 0, data.Length);
        }

        /// <summary>
        /// 开始异步发送消息,原生数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        private void SocketSend(byte[] data, int start, int len)
        {
            lock (this)//多线程问题，防止争夺send
            {
                if (_socket!=null && _socket.Connected)
                {
                    _socket.BeginSend(data, start, len, SocketFlags.None, new AsyncCallback(SendCallback), _socket);
                }
            }
        }

        /// <summary>
        /// 异步发送消息回调
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            if (_socket != null && _socket.Connected)
            {
                // 发送的字节数
                int len = _socket.EndSend(ar);
            }

        }

        #endregion

    }
}
