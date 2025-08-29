using Serilog;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.Summer.Net
{
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
        // 状态字段
        private bool m_isStart = false;                  // 解码器是否已经启动
        private Socket m_Socket;
        private Memory<byte> m_bufferMem;               
        private int m_offset = 0;                        // 缓冲区目前的长度

        // 配置参数
        private int m_maxBufferLength;              // 一次性接收数据的最大字节，默认64kMb
        private int m_lengthFieldOffset;            // 第几个字节是长度字段
        private int m_lengthFieldLength;            // 长度字段本身占几个字节
        private int m_lengthAdjustment;             // 长度字段和内容之间距离几个字节（也就是长度字段记录了整一个数据包的长度，负数代表向前偏移，body实际长度要减去这个绝对值）
        private int m_initialBytesToStrip;          // 表示获取完一个完整的数据包之后，舍弃前面的多少个字节
        
        // 委托事件
        public delegate void ReceivedHandler(ReadOnlyMemory<byte> data);
        public delegate void DisconnectedHandler();
        private event ReceivedHandler m_onDataRecived;
        private event DisconnectedHandler m_onDisconnected;

        public LengthFieldDecoder(
            Socket socket, 
            int maxBufferLength = 64 * 1024, 
            int lengthFieldOffset = 0,
            int lengthFieldLength = 4,
            int lengthAdjustment = 0, 
            int initialBytesToStrip = 4, 
            ReceivedHandler onDataRecived = null, 
            DisconnectedHandler onDisconnected = null)
        {
            m_Socket = socket;
            m_maxBufferLength = maxBufferLength;
            this.m_lengthFieldOffset = lengthFieldOffset;
            this.m_lengthFieldLength = lengthFieldLength;
            this.m_lengthAdjustment = lengthAdjustment;
            this.m_initialBytesToStrip = initialBytesToStrip;
            m_bufferMem = new Memory<byte>(new byte[m_maxBufferLength]);
            m_onDataRecived = onDataRecived;
            m_onDisconnected = onDisconnected;
        }
        
        public async Task StartAsync()
        {
            if (m_Socket != null && !m_isStart)
            {
                m_isStart = true;
                int bytesRead = 0;
                try
                {
                    while (m_isStart)
                    {
                        var slice = m_bufferMem.Slice(m_offset);
                        bytesRead = await m_Socket.ReceiveAsync(slice, SocketFlags.None);
                        if (bytesRead == 0)
                        {
                            // 消息长度为0，代表连接已经断开
                            _PassiveDisconnection();
                            break;
                        }
                        _ProcessReceivedData(bytesRead);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "接收数据时发生异常");
                    _PassiveDisconnection();
                }
            }
        }
        public void ActiveDisconnection()
        {
            m_isStart = false;
            _PassiveDisconnection();
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

            // 并且向上传递消息断开信息
            if (m_isStart)
            {
                m_isStart = false;
                m_onDisconnected?.Invoke();
            }
        }
        private void _ProcessReceivedData(int len)
        {
            int headLen = m_lengthFieldOffset + m_lengthFieldLength;

            // 获取对应内存视图
            Span<byte> bufferSpan = m_bufferMem.Span;
            m_offset += len;

            while (true)
            {
                // 1. 缓冲区溢出保护
                if (m_offset > m_bufferMem.Length)
                {
                    throw new InvalidOperationException($"Buffer overflow. Offset:{m_offset} Capacity:{m_bufferMem.Length}");
                }

                // 2. 使用结构化条件判断
                bool hasEnoughHeader = m_offset >= headLen;
                if (!hasEnoughHeader) return;

                // 3. 安全读取长度字段
                if (!TryReadLengthField(bufferSpan, out int bodyLen))
                {
                    Log.Warning("Invalid length field format");
                    return;
                }

                // 4. 计算总包长度并验证
                int expectedTotalLength = headLen + m_lengthAdjustment + bodyLen;
                if (expectedTotalLength > m_bufferMem.Length)
                {
                    throw new InvalidDataException($"Package too large. Length:{expectedTotalLength} Max:{m_bufferMem.Length}");
                }

                if (m_offset < expectedTotalLength) return;

                // 5. 提取有效载荷（优化内存分配）
                int payloadStart = m_initialBytesToStrip;
                int payloadLength = bodyLen - (m_initialBytesToStrip - m_lengthFieldLength);
                Memory<byte> payloadMem = m_bufferMem.Slice(payloadStart, payloadLength);

                // 6. 触发事件（传递Memory避免复制）
                m_onDataRecived?.Invoke(payloadMem);

                // 7. 滑动缓冲区（高性能方式）
                int remainingData = m_offset - expectedTotalLength;
                if (remainingData > 0)
                {
                    // 将剩余数据移动到缓冲区起始位置
                    bufferSpan.Slice(expectedTotalLength, remainingData).CopyTo(bufferSpan);
                }
                m_offset = remainingData;

                // 8. 零长度退出机制
                if (remainingData == 0) break;
            }
        }

        // 安全读取大端长度字段
        private bool TryReadLengthField(Span<byte> buffer, out int length)
        {
            try
            {
                length = BinaryPrimitives.ReadInt32BigEndian(
                    buffer.Slice(m_lengthFieldOffset, m_lengthFieldLength));
                return length >= 0;
            }
            catch (ArgumentOutOfRangeException)
            {
                length = -1;
                return false;
            }
        }
    }
}