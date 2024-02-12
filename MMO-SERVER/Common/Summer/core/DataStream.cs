using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Summer.Core
{
    /// <summary>
    /// 数据流,方便读写的操作,主要用于他data添加头部的字段长度
    /// 采用大端模式存储
    /// </summary>
    public class DataStream : MemoryStream
    {
        private static Queue<DataStream> pool = new Queue<DataStream>();//池化技术
        public static int PoolMaxCount = 200;

        private DataStream() { }


        public static DataStream Allocate()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    return pool.Dequeue();
                }
            }
            return new DataStream();
        }

        public static DataStream Allocate(byte[] bytes)
        {
            DataStream stream = Allocate();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;//读取位置
            return stream;
        }

        /// <summary>
        /// 当流释放的时候就会回调这个方法
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            //Log.Information("DataStream自动释放");
            lock (pool)
            {
                if (pool.Count < PoolMaxCount)
                {
                    this.Position = 0;
                    this.SetLength(0);
                    pool.Enqueue(this);
                    //Console.WriteLine("DataStream池子长度：" + pool.Count);
                }
                else
                {
                    this.Dispose(disposing);
                    this.Close();
                }
            }

        }


        public ushort ReadUShort()
        {
            byte[] bytes = new byte[2];
            this.Read(bytes, 0, 2);
            if(BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes);
        }

        public uint ReadUInt()
        {
            byte[] bytes = new byte[4];
            this.Read(bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes);
        }

        public ulong ReadULong()
        {
            byte[] bytes = new byte[8];
            this.Read(bytes, 0, 8);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes);
        }


        public void WriteUShort(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value); 
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            this.Write(bytes, 0, 2);
        }

        public void WriteInt(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            this.Write(bytes, 0, 4);
        }


        public void WriteLong(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            this.Write(bytes, 0, 8);
        }



    }
}
