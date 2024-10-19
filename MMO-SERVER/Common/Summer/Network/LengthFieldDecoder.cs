using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameServer.Network
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
        private bool isStart = false;           //解码器是否已经启动
        private Socket mSocket;                 //连接客户端的socket
        private int lengthFieldOffset =0;       //第几个字节是长度字段
        private int lengthFieldLength = 4;      //长度字段本身占几个字节
        private int lengthAdjustment = 0;       //长度字段和内容之间距离几个字节（也就是长度字段记录了整一个数据包的长度，负数代表向前偏移，body实际长度要减去这个绝对值）
        private int initialBytesToStrip = 4;    //表示获取完一个完整的数据包之后，舍弃前面的多少个字节
        private byte[] mBuffer;                 //接收数据的缓存空间
        private int mOffect = 0;                //缓冲区目前的长度
        private int mSize = 64 * 1024;          //一次性接收数据的最大字节，默认64kMb

        //委托事件
        public delegate void ReceivedHandler(byte[] data);
        public delegate void DisconnectedHandler();
        public event ReceivedHandler Received;
        public event DisconnectedHandler Disconnected;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="socket">客户端socket</param>
        /// <param name="maxBufferLength"></param>
        /// <param name="lengthFieldOffset"></param>
        /// <param name="lengthFieldLength"></param>
        /// <param name="lengthAdjustment"></param>
        /// <param name="initialBytesToStrip"></param>
        public LengthFieldDecoder(Socket socket, int maxBufferLength, int lengthFieldOffset, int lengthFieldLength,int lengthAdjustment, int initialBytesToStrip)
        {
            mSocket = socket;
            mSize = maxBufferLength;
            this.lengthFieldOffset = lengthFieldOffset;
            this.lengthFieldLength = lengthFieldLength;
            this.lengthAdjustment = lengthAdjustment;
            this.initialBytesToStrip = initialBytesToStrip;
            mBuffer = new byte[mSize];
        }

        /// <summary>
        /// 启动解码器
        /// </summary>
        public void Start()
        {
            if (mSocket != null && !isStart)
            {
                mSocket.BeginReceive(mBuffer, mOffect, mSize - mOffect, SocketFlags.None, new AsyncCallback(OnReceive), null);
                isStart = true;
            }
        }

        /// <summary>
        /// 异步接收的回调
        /// </summary>
        /// <param name="result"></param>
        public void OnReceive(IAsyncResult result)
        {
            try
            {
                int len = 0;
                if(mSocket != null)
                {
                    len = mSocket.EndReceive(result);
                }

                // 消息长度为0，代表连接已经断开
                if (len == 0)
                {
                    PassiveDisconnection();
                    return;
                }

                //处理信息
                ReadMessage(len);

                //继续接收数据
                mSocket.BeginReceive(mBuffer, mOffect, mSize - mOffect, SocketFlags.None, new AsyncCallback(OnReceive), null);

            }
            catch (ObjectDisposedException e)
            {
                // Socket 已经被释放
                Log.Information("[Socket 已释放，接收操作中止。]");
                Log.Information(e.ToString());
                PassiveDisconnection();
            }
            catch (SocketException e)
            {
                //打印一下异常，并且断开与客户端的连接
                Log.Information("[SocketException]");
                Log.Information(e.ToString());
                PassiveDisconnection();
            } catch (Exception e)
            {
                //打印一下异常，并且断开与客户端的连接
                Log.Information(e.ToString());
                PassiveDisconnection();
            }

        }

        /// <summary>
        /// 处理数据，并且进行转发处理
        /// </summary>
        /// <param name="len"></param>
        private void ReadMessage(int len)
        {

            //headLen+bodyLen=totalLen

            int headLen = lengthFieldOffset + lengthFieldLength;//魔法值+长度字段的长度
            int adj = lengthAdjustment; //body偏移量


            //循环开始之前mOffect代表上次剩余长度
            //所以moffect需要加上本次送过来的len
            mOffect += len;
           

            //循环解析
            while (true)
            {
                //此时缓冲区内有moffect长度的字节需要去处理

                //如果未处理的数据超出缓冲区大小限制
                if (mOffect > mSize)
                {
                    throw new IndexOutOfRangeException("数据超出限制");
                }
                if (mOffect < headLen)
                {
                    //接收的数据不够一个完整的包，继续接收
                    return;
                }

                //获取body长度，通过大端模式
                //int bodyLen = BitConverter.ToInt32(mBuffer, lengthFieldOffset);
                int bodyLen = GetInt32BE(mBuffer, lengthFieldOffset);

                //判断body够不够长
                if (mOffect < headLen + adj + bodyLen)
                {
                    //接收的数据不够一个完整的包，继续接收
                    return;
                }

                //整个包的长度为
                int total = headLen + bodyLen;



                //获取数据
                byte[] data = new byte[bodyLen];
                Array.Copy(mBuffer, headLen, data, 0, bodyLen);

                //数据解析完毕就需要更新buffer缓冲区
                Array.Copy(mBuffer, bodyLen+ headLen, mBuffer, 0, mOffect- total);
                mOffect = mOffect - total;

                //完成一个数据包
                Received(data);
            }

        }
        /// <summary>
        /// 获取大端模式int值
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int GetInt32BE(byte[] data, int index)
        {
            return (data[index] << 0x18) | (data[index + 1] << 0x10) | (data[index + 2] << 8) | (data[index + 3]);
        }

        /// <summary>
        /// 被动关闭连接
        /// </summary>
        private void PassiveDisconnection()
        {

            try
            {
                mSocket?.Shutdown(SocketShutdown.Both);
                mSocket?.Close();
                mSocket?.Dispose();
            }
            catch { 
                
            } 
            mSocket = null;

            //并且向上传递消息断开信息
            if (isStart)
            {
                Disconnected?.Invoke();
            }
        }

        /// <summary>
        /// 主动关闭连接
        /// </summary>
        public void ActiveClose()
        {
            isStart = false;
            PassiveDisconnection();
        }

    }
}
