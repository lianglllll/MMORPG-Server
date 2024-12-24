using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Summer.Tools
{


    /// <summary>
    /// varints整数压缩和解码
    /// </summary>
    public class Varint
    {

        /// <summary>
        /// varint压缩
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] VarintEncode(ulong value)
        {
            var list = new List<byte>();
            while (value > 0)
            {
                byte b = (byte)(value & 0x7f);  //获取value最低的7位
                value >>= 7;                    //value右移7位，丢弃
                if (value > 0)
                {   
                    b |= 0x80;                  //如果还有剩余的位需要编码，将b的最高位置为1
                }
                list.Add(b);                    
            }
            return list.ToArray();
        }

        /// <summary>
        /// varint 解析
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static ulong VarintDecode(byte[] buffer)
        {
            ulong value = 0;
            int shift = 0;
            int len = buffer.Length;
            for (int i = 0; i < len; i++)
            {
                byte b = buffer[i];
                value |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }
            return value;
        }


        /// <summary>
        /// 判断有效字节的数量
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int VarintSize(ulong value)
        {
            //位置7位，如果前面都为0，说明只有一个有效字节
            if ((value & (0xFFFFFFFF << 7)) == 0)
            {
                return 1;
            }

            if ((value & (0xFFFFFFFF << 14)) == 0)
            {
                return 2;
            }

            if ((value & (0xFFFFFFFF << 21)) == 0)
            {
                return 3;
            }

            if ((value & (0xFFFFFFFF << 28)) == 0)
            {
                return 4;
            }
            return 5;
        }

    }
}
