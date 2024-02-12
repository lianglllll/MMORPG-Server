using System;
using System.Collections.Generic;
using System.IO;
using System.Text;



namespace Summer
{
    public class DataSerializer
    {
        //sbyte,ushort,uint,ulong
        //byte,short,int,long,float,double,bool,string
        //对象数组转为byte数组
        public static byte[] Serialize(params object? []? args)
        {
            using (var stream = new MemoryStream())
            {
                foreach (var arg in args)
                {
                    if (arg is null) { stream.WriteByte(0); }
                    else if (arg is sbyte)
                    {
                        stream.WriteByte((byte)1);//用一个整数来标志数据类型
                        stream.WriteByte((byte)(sbyte)arg);
                    }
                    else if (arg is byte)
                    {
                        stream.WriteByte((byte)2);
                        stream.WriteByte((byte)arg);
                    }
                    else if (arg is short)
                    {
                        stream.WriteByte((byte)3);
                        byte[] arr = BitConverter.GetBytes((short)arg);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is ushort)
                    {
                        stream.WriteByte((byte)4);
                        byte[] arr = BitConverter.GetBytes((ushort)arg);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is int)
                    {
                        stream.WriteByte((byte)5);
                        byte[] arr = Varint.VarintEncode((ulong)(int)arg);
                        stream.WriteByte((byte)arr.Length);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is uint)
                    {
                        stream.WriteByte((byte)6);
                        byte[] arr = Varint.VarintEncode((uint)arg);
                        stream.WriteByte((byte)arr.Length);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is long)
                    {
                        stream.WriteByte((byte)7);
                        var d = (long)arg;
                        byte[] arr = Varint.VarintEncode((ulong)d);
                        stream.WriteByte((byte)arr.Length);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is ulong)
                    {
                        stream.WriteByte((byte)8);
                        byte[] arr = Varint.VarintEncode((ulong)arg);
                        stream.WriteByte((byte)arr.Length);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is float)
                    {
                        stream.WriteByte((byte)9);
                        float d = 1000f * (float)arg;
                        byte[] arr = Varint.VarintEncode((ulong)(long)d);
                        stream.WriteByte((byte)arr.Length);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is double)
                    {
                        stream.WriteByte((byte)10);
                        double d = 1000d * (double)arg;
                        byte[] arr = Varint.VarintEncode((ulong)(long)d);
                        stream.WriteByte((byte)arr.Length);
                        stream.Write(arr, 0, arr.Length);
                    }
                    else if (arg is bool)
                    {
                        bool b = (bool)arg;
                        stream.WriteByte((byte)(b ? 11 : 12));
                    }
                    else if (arg is string)//如果是字符串的话也转换成整型来传输
                    {
                        string d = (string)arg;
                        byte[] data = Encoding.UTF8.GetBytes(d);
                        byte[] lenBytes = BitConverter.GetBytes((int)data.Length);//一个int型的长度
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(lenBytes);
                        stream.WriteByte((byte)13);
                        stream.Write(lenBytes, 0, lenBytes.Length);
                        stream.Write(data, 0, data.Length);
                        //Log.Debug("lenBytes.len={0},data.len={1}", lenBytes.Length, data.Length);
                    }
                    else
                    {
                        throw new ArgumentException("无法处理的数据类型：" + arg.GetType());
                    }
                }
                return stream.ToArray();
            }
            //return null;
        }

        //反序列化
        public static object[] Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                var list = new List<object>();
                while (stream.Position < stream.Length)
                {
                    byte type = (byte)stream.ReadByte();
                    if (type == 0) { list.Add(null); }
                    else if (type == 1)
                    {
                        list.Add((sbyte)stream.ReadByte());
                    }
                    else if (type == 2)
                    {
                        list.Add((byte)stream.ReadByte());
                    }
                    else if (type == 3) //short
                    {
                        byte[] arr = new byte[2];
                        stream.Read(arr, 0, 2);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        list.Add(BitConverter.ToInt16(arr, 0));
                    }
                    else if (type == 4) //ushort
                    {
                        byte[] arr = new byte[2];
                        stream.Read(arr, 0, 2);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        list.Add(BitConverter.ToUInt16(arr, 0));
                    }
                    else if (type == 5) //int
                    {
                        int len = stream.ReadByte();
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, len);

                        list.Add((int)Varint.VarintDecode(arr));
                    }
                    else if (type == 6) //uint
                    {
                        int len = stream.ReadByte();
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, len);
                        list.Add((uint)Varint.VarintDecode(arr));
                    }
                    else if (type == 7) //long
                    {
                        int len = stream.ReadByte();
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, len);
                        ulong r = Varint.VarintDecode(arr);

                        list.Add((long)r);
                    }
                    else if (type == 8) //ulong
                    {
                        int len = stream.ReadByte();
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, len);
                        list.Add((ulong)Varint.VarintDecode(arr));
                    }
                    else if (type == 9) //float
                    {
                        int len = stream.ReadByte();
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, len);
                        float value = Varint.VarintDecode(arr) / 1000.0f;
                        list.Add(value);
                    }
                    else if (type == 10) //double
                    {
                        int len = stream.ReadByte();
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, len);
                        double value = Varint.VarintDecode(arr) / 1000.0d;
                        list.Add(value);
                    }
                    else if (type == 11) //bool-true
                    {
                        list.Add(true);
                    }
                    else if (type == 12) //bool-false
                    {
                        list.Add(false);
                    }
                    else if (type == 13) //string
                    {
                        byte[] lenBytes = new byte[4];
                        stream.Read(lenBytes, 0, lenBytes.Length);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(lenBytes);
                        int len = BitConverter.ToInt32(lenBytes);
                        // Log.Debug("字符串长度：{0}", len);
                        byte[] arr = new byte[len];
                        stream.Read(arr, 0, arr.Length);
                        list.Add(Encoding.UTF8.GetString(arr));
                    }
                    else
                    {
                        throw new ArgumentException("无法处理的数据类型：" + type);
                    }

                }

                return list.ToArray();
            }
        }

    }
}







#region 貌似没啥用

/*查找7位到8位的过渡值
 long min = 0;
long max = long.MaxValue;
while(true)
{
    long m = min + (max - min) /2;
    byte[] arr = DataSerializer.VarintEncode(m);
    byte[] arr2 = DataSerializer.VarintEncode(m + 1);
    if (arr.Length==7&& arr2.Length == 8)
    {
        Log.Information("结果={0}", m);
        break;
    }

    if (arr.Length < 7)
    {
        min = m;
    }
    if (arr.Length >= 8)
    {
        max = m;
    }

}

Log.Information("{0}", DataSerializer.VarintEncode(562949953421311).Length);
Log.Information("{0}", DataSerializer.VarintEncode(562949953421312).Length);
 */

//VarintEncode
/* public static byte[] VarintEncode(ulong value)
 {
     var list = new List<byte>();
     while (value > 0)
     {
         byte b = (byte)(value & 0x7f);
         value >>= 7;
         if (value > 0)
         {
             b |= 0x80;
         }
         list.Add(b);
     }
     return list.ToArray();
 }

 //VarintDecode
 public static ulong VarintDecode(byte[] data)
 {
     ulong value = 0;
     for (int i = 0; i < data.Length; i++)
     {
         byte b = data[i];
         value |= (ulong)(b & 0x7f) << (i * 7);
         if ((b & 0x80) == 0)
         {
             break;
         }
     }
     return value;
 }

 //VarintEncode
 public static byte[] VarintEncode(long value)
 {
     var list = new List<byte>();
     while (value > 0)
     {
         byte b = (byte)(value & 0x7f);
         value >>= 7;
         if (value > 0)
         {
             b |= 0x80;
         }
         list.Add(b);
     }
     return list.ToArray();
 }

 //VarintDecode
 public static long VarintDecode(byte[] data)
 {
     long value = 0;
     for (int i = 0; i < data.Length; i++)
     {
         byte b = data[i];
         value |= (long)(b & 0x7f) << (i * 7);
         if ((b & 0x80) == 0)
         {
             break;
         }
     }
     return value;
 }

 //VarintEncode
 public static byte[] VarintEncode(uint value)
 {
     var list = new List<byte>();
     while (value > 0)
     {
         byte b = (byte)(value & 0x7f);
         value >>= 7;
         if (value > 0)
         {
             b |= 0x80;
         }
         list.Add(b);
     }
     return list.ToArray();
 }

 //VarintDecode
 public static uint VarintDecode(byte[] data)
 {
     uint value = 0;*/
#endregion
