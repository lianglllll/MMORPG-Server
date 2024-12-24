using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Summer.Tools
{
    /// <summary>
    /// 池化数据流
    /// </summary>
    public class DataStream : MemoryStream
    {
        //对象池
        private static ConcurrentQueue<DataStream> pool = new();
        public static int PoolMaxCount = 200;
        public static bool IsLittleEndian = false; //是否小端模式

        private DataStream()
        {
        }

        /// <summary>
        /// 从对象池中获取DataStream
        /// </summary>
        public static DataStream Allocate()
        {
            //从对象池中获取
            if (pool.TryDequeue(out var stream))
            {
                stream.SetLength(0);
                //stream.Seek(0, SeekOrigin.Begin);
                stream.Position = 0;
                return stream;
            }
            //如果对象池中没有则创建一个新的
            return new DataStream();
        }

        public static DataStream Allocate(byte[] bytes)
        {
            DataStream stream = Allocate();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 释放资源 || 回收资源
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            //Log.Information("DataStream自动释放");
            if (pool.Count < PoolMaxCount)
            {
                Position = 0;
                SetLength(0);
                //this.Seek(0, SeekOrigin.Begin);
                pool.Enqueue(this);
            }
            else
            {
                base.Dispose(disposing);
            }

        }




        #region 自定义读写 


        public void Write0(byte[] buffer, int offset, int count)
        {
            if (IsLittleEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer, offset, count);
            }
            base.Write(buffer, offset, count);
        }

        public int Read0(byte[] buffer, int offset, int count)
        {
            int bytesRead = base.Read(buffer, offset, count);
            if (IsLittleEndian != BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer, offset, bytesRead);
            }
            return bytesRead;
        }


        // 写入一个 sbyte
        public void WriteSByte(sbyte value)
        {
            WriteByte((byte)value);
        }

        // 读取一个 sbyte
        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        // 写入一个 ushort
        public void WriteUInt16(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }
        public void WriteUShort(ushort value)
        {
            WriteUInt16(value);
        }

        // 读取一个 ushort
        public ushort ReadUInt16()
        {
            byte[] buffer = new byte[sizeof(ushort)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToUInt16(buffer, 0);
        }
        public ushort ReadUShort()
        {
            return ReadUInt16();
        }

        // 写入一个 uint
        public void WriteUInt32(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }

        // 读取一个 uint
        public uint ReadUInt32()
        {
            byte[] buffer = new byte[sizeof(uint)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToUInt32(buffer, 0);
        }

        // 写入一个 ulong
        public void WriteUInt64(ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }

        // 读取一个 ulong
        public ulong ReadUInt64()
        {
            byte[] buffer = new byte[sizeof(ulong)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToUInt64(buffer, 0);
        }

        // 写入一个 short
        public void WriteInt16(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }

        // 读取一个 short
        public short ReadInt16()
        {
            byte[] buffer = new byte[sizeof(short)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToInt16(buffer, 0);
        }

        // 写入一个 int
        public void WriteInt32(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }
        public void WriteInt(int value)
        {
            WriteInt32(value);
        }

        // 读取一个 int
        public int ReadInt32()
        {
            byte[] buffer = new byte[sizeof(int)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToInt32(buffer, 0);
        }

        // 写入一个 long
        public void WriteInt64(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }

        // 读取一个 long
        public long ReadInt64()
        {
            byte[] buffer = new byte[sizeof(long)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToInt64(buffer, 0);
        }

        // 写入一个 float
        public void WriteSingle(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }

        // 读取一个 float
        public float ReadSingle()
        {
            byte[] buffer = new byte[sizeof(float)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToSingle(buffer, 0);
        }

        // 写入一个 double
        public void WriteDouble(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Write0(bytes, 0, bytes.Length);
        }

        // 读取一个 double
        public double ReadDouble()
        {
            byte[] buffer = new byte[sizeof(double)];
            Read0(buffer, 0, buffer.Length);
            return BitConverter.ToDouble(buffer, 0);
        }

        // 写入一个 bool
        public void WriteBoolean(bool value)
        {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        // 读取一个 bool
        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        // 写入一个短文本（字节长度小于65535）
        public void WriteText(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            ushort length = (ushort)bytes.Length;
            if (length > ushort.MaxValue)
            {
                length = ushort.MaxValue;
            }
            WriteUInt16(length);
            Write(bytes, 0, length);
        }

        // 写入一个长文本（字节长度小于 int.MaxValue）
        public void WriteLongText(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteInt32(bytes.Length);
            Write(bytes, 0, bytes.Length);
        }
        // 读取短文本
        public string ReadText()
        {
            ushort length = ReadUInt16();
            byte[] bytes = new byte[length];
            Read(bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        // 读取长文本
        public string ReadLongText()
        {
            int length = ReadInt32();
            byte[] bytes = new byte[length];
            Read(bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        #endregion

        #region 重载Write方法

        // 写入一个 sbyte
        public void Write(sbyte value)
        {
            WriteSByte(value);
        }

        // 写入一个 ushort
        public void Write(ushort value)
        {
            WriteUInt16(value);
        }

        // 写入一个 uint
        public void Write(uint value)
        {
            WriteUInt32(value);
        }

        // 写入一个 ulong
        public void Write(ulong value)
        {
            WriteUInt64(value);
        }

        // 写入一个 byte
        public void Write(byte value)
        {
            WriteByte(value);
        }

        // 写入一个 short
        public void Write(short value)
        {
            WriteInt16(value);
        }

        // 写入一个 int
        public void Write(int value)
        {
            WriteInt32(value);
        }

        // 写入一个 long
        public void Write(long value)
        {
            WriteInt64(value);
        }

        // 写入一个 float
        public void Write(float value)
        {
            WriteSingle(value);
        }

        // 写入一个 double
        public void Write(double value)
        {
            WriteDouble(value);
        }

        // 写入一个 bool
        public void Write(bool value)
        {
            WriteBoolean(value);
        }

        public byte[] ReadBytes(int len)
        {
            byte[] buffer = new byte[len];
            Read(buffer, 0, len);
            return buffer;
        }

        #endregion
    }
}
