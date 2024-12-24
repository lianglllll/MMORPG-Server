using Serilog;
using System;
using System.Net.Sockets;

namespace Common.Summer.Net
{
    //todo 这里的异步模型还是begin/end 我们改成async
    /// <summary>
    /// 这是Socket异步接收器，可以对接收的数据粘包与拆包
    /// 事件委托：
    ///     -- Received 数据包接收完成事件，参数为接收的数据包
    ///     -- ConnectFailed 接收异常事件
    ///     
    /// 使用方法：
    ///     var lfd  = new LengthFieldDecoder(socket,64*64,0,4,0,4)
    /// </summary>
    /// 这里我们的数据格式是：4字节长度字段+数据部分
    ///                     而数据部分的前两个字节是我们的proto协议的编号
    public class LengthFieldDecoder
    {
        private bool m_isStart = false;             //解码器是否已经启动
        private Socket m_Socket;                    //连接客户端的socket
        private int m_lengthFieldOffset =0;         //第几个字节是长度字段
        private int m_lengthFieldLength = 4;        //长度字段本身占几个字节
        private int m_lengthAdjustment = 0;         //长度字段和内容之间距离几个字节（也就是长度字段记录了整一个数据包的长度，负数代表向前偏移，body实际长度要减去这个绝对值）
        private int m_initialBytesToStrip = 4;      //表示获取完一个完整的数据包之后，舍弃前面的多少个字节
        private byte[] m_buffer;                    //接收数据的缓存空间
        private int m_offect = 0;                   //缓冲区目前的长度
        private int m_size = 64 * 1024;             //一次性接收数据的最大字节，默认64kMb

        //委托事件
        public delegate void ReceivedHandler(byte[] data);
        public delegate void DisconnectedHandler();
        private event ReceivedHandler m_onDataRecived;
        private event DisconnectedHandler m_onDisconnected;

        public LengthFieldDecoder(Socket socket, int maxBufferLength, int lengthFieldOffset,
            int lengthFieldLength,int lengthAdjustment, int initialBytesToStrip, 
            ReceivedHandler onDataRecived, DisconnectedHandler onDisconnected)
        {
            m_Socket = socket;
            m_size = maxBufferLength;
            this.m_lengthFieldOffset = lengthFieldOffset;
            this.m_lengthFieldLength = lengthFieldLength;
            this.m_lengthAdjustment = lengthAdjustment;
            this.m_initialBytesToStrip = initialBytesToStrip;
            m_buffer = new byte[m_size];
            m_onDataRecived = onDataRecived;
            m_onDisconnected = onDisconnected;
        }

        public void Init()
        {
            if (m_Socket != null && !m_isStart)
            {
                m_isStart = true;
                m_Socket.BeginReceive(m_buffer, m_offect, m_size - m_offect, SocketFlags.None, new AsyncCallback(OnReceive), null);
            }
        }
        private void _PassiveDisconnection()
        {
            try
            {
                m_Socket?.Shutdown(SocketShutdown.Both); //停止数据发送和接收，确保正常关闭连接。
                m_Socket?.Close();                       //关闭 Socket 并释放其资源
                //mSocket?.Dispose();                     //释放 Socket 占用的所有资源，特别是非托管资源。（Close已经隐式调用了）
            }
            catch
            {

            }
            m_Socket = null;

            //并且向上传递消息断开信息
            if (m_isStart)
            {
                m_onDisconnected?.Invoke();
            }
        }
        public void ActiveDisconnection()
        {
            m_isStart = false;
            _PassiveDisconnection();
        }
        public void OnReceive(IAsyncResult result)
        {
            try
            {
                int len = 0;
                if (m_Socket != null && m_Socket.Connected)
                {
                    len = m_Socket.EndReceive(result);
                }

                // 消息长度为0，代表连接已经断开
                if (len == 0)
                {
                    _PassiveDisconnection();
                    return;
                }

                //处理信息
                _ReadMessage(len);

                // 继续接收数据
                if (m_Socket != null && m_Socket.Connected)
                {
                    m_Socket.BeginReceive(m_buffer, m_offect, m_size - m_offect, SocketFlags.None, new AsyncCallback(OnReceive), null);
                }
                else
                {
                    Log.Information("[LengthFieldDecoder]Socket 已断开连接，无法继续接收数据。");
                    _PassiveDisconnection();
                }

            }
            catch (ObjectDisposedException e)
            {
                // Socket 已经被释放
                Log.Information("[LengthFieldDecoder:ObjectDisposedException]");
                Log.Information(e.ToString());
                _PassiveDisconnection();
            }
            catch (SocketException e)
            {
                //打印一下异常，并且断开与客户端的连接
                Log.Information("[[LengthFieldDecoder:SocketException]");
                Log.Information(e.ToString());
                _PassiveDisconnection();
            }
            catch (Exception e)
            {
                //打印一下异常，并且断开与客户端的连接
                Log.Information("[LengthFieldDecoder:Exception]");
                Log.Information(e.ToString());
                _PassiveDisconnection();
            }
        }
        // 处理数据，并且进行转发处理
        private void _ReadMessage(int len)
        {
            //headLen+bodyLen=totalLen

            int headLen = m_lengthFieldOffset + m_lengthFieldLength;//魔法值+长度字段的长度
            int adj = m_lengthAdjustment; //body偏移量

            //循环开始之前mOffect代表上次剩余长度
            //所以moffect需要加上本次送过来的len
            m_offect += len;

            //循环解析
            while (true)
            {
                //此时缓冲区内有moffect长度的字节需要去处理

                //如果未处理的数据超出缓冲区大小限制
                if (m_offect > m_size)
                {
                    throw new IndexOutOfRangeException("数据超出限制");
                }
                if (m_offect < headLen)
                {
                    //接收的数据不够一个完整的包，继续接收
                    return;
                }

                //获取body长度，通过大端模式
                int bodyLen = _GetInt32BE(m_buffer, m_lengthFieldOffset);

                //判断body够不够长
                if (m_offect < headLen + adj + bodyLen)
                {
                    //接收的数据不够一个完整的包，继续接收
                    return;
                }

                //整个包的长度为
                int total = headLen + bodyLen;

                //获取数据
                byte[] data = new byte[bodyLen];
                Array.Copy(m_buffer, headLen, data, 0, bodyLen);

                //数据解析完毕就需要更新buffer缓冲区
                Array.Copy(m_buffer, bodyLen+ headLen, m_buffer, 0, m_offect- total);
                m_offect = m_offect - total;

                //完成一个数据包
                m_onDataRecived?.Invoke(data);
            }

        }
        // 获取大端模式int值
        private int _GetInt32BE(byte[] data, int index)
        {
            return (data[index] << 0x18) | (data[index + 1] << 0x10) | (data[index + 2] << 8) | (data[index + 3]);
        }
    }
}