using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Common.Summer.Core;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
using UnityEngine;
#endif

namespace Common.Summer.Tools
{


    public class DataConvert
    {
        //sbyte,ushort,uint,ulong
        //byte,short,int,long,float,double,bool,string
        //数组、字典、Vector3、Vector2

        public enum Endianess
        {
            LittleEndian,
            BigEndian
        }

        //默认大端模式
        public static Endianess EndianMode = Endianess.BigEndian;
        public static byte[] Serialize(params object?[]? args)
        {
            using var stream = DataStream.Allocate();
            SerializeData(stream, args);
            return stream.ToArray();
        }
        private static void SerializeData(DataStream stream, params object?[]? args)
        {
            var writer = stream;
            foreach (var arg in args)
            {
                Log.Information("类型={0}", arg.GetType());
                if (arg is null)
                {
                    writer.Write((byte)0);
                }
                else if (arg is sbyte)
                {
                    writer.Write((byte)1);
                    writer.Write((sbyte)arg);
                }
                else if (arg is byte)
                {
                    writer.Write((byte)2);
                    writer.Write((byte)arg);
                }
                else if (arg is short)
                {
                    short n = (short)arg;
                    if (n >= 0 && n <= 255)
                    {
                        writer.Write((byte)3);
                        writer.Write((byte)n);
                    }
                    else if(n >= -255 && n <= -1) //-
                    {
                        writer.Write((byte)4);
                        writer.Write((byte)-n);
                    }
                    else
                    {
                        writer.Write((byte)5);
                        writer.Write((short)n);
                    }
                }
                else if (arg is ushort)
                {
                    ushort u = (ushort)arg;
                    if (u <= 255)
                    {
                        writer.Write((byte)6);
                        writer.Write((byte)u);
                    }
                    else
                    {
                        writer.Write((byte)7);
                        writer.Write((ushort)u);
                    }
                }
                else if (arg is int)
                {
                    int n = (int)arg;
                    if (n >= 0 && n <= 255)
                    {
                        writer.Write((byte)8);
                        writer.Write((byte)n);
                    }
                    else if (n >= 256 && n <= 65535)
                    {
                        writer.Write((byte)9);
                        writer.Write((ushort)n);
                    }
                    else if (n >= -255 && n <= -1) //-
                    {
                        writer.Write((byte)10);
                        writer.Write((byte)-n);
                    }
                    else if (n >= -65535 && n <= -256) //-
                    {
                        writer.Write((byte)11);
                        writer.Write((ushort)-n);
                    }
                    else
                    {
                        writer.Write((byte)12);
                        writer.Write((int)n);
                    }
                }
                else if (arg is uint)
                {
                    uint n = (uint)arg;
                    if (n <= 255)
                    {
                        writer.Write((byte)13);
                        writer.Write((byte)n);
                    }
                    else if (n >= 256 && n <= 65535)
                    {
                        writer.Write((byte)14);
                        writer.Write((ushort)n);
                    }
                    else
                    {
                        writer.Write((byte)15);
                        writer.Write((uint)arg);
                    }
                    
                }
                else if (arg is long)
                {
                    long n = (long)arg;
                    if (n >= 0 && n <= 255)
                    {
                        writer.Write((byte)16);
                        writer.Write((byte)n);
                    }
                    else if (n >= -255 && n <= -1) //-
                    {
                        writer.Write((byte)17);
                        writer.Write((byte)-n);
                    }
                    else if (n >= 256 && n <= 65535)
                    {
                        writer.Write((byte)18);
                        writer.Write((ushort)n);
                    }
                    else if (n >= -65535 && n <= -256)
                    {
                        writer.Write((byte)19); //-
                        writer.Write((ushort)-n);
                    }
                    else if (n >= 65535 && n <= uint.MaxValue)
                    {
                        writer.Write((byte)20);
                        writer.Write((uint)n);
                    }
                    else if (n <= -65535 && n >= -uint.MaxValue)
                    {
                        writer.Write((byte)21);//-
                        writer.Write((uint)-n);
                    }
                    else
                    {
                        writer.Write((byte)22);
                        writer.Write(n);
                    }
                }
                else if (arg is ulong)
                {
                    ulong n = (ulong)arg;
                    if (n <= 255)
                    {
                        writer.Write((byte)23);
                        writer.Write((byte)n);
                    }
                    else if (n >= 256 && n <= 65535)
                    {
                        writer.Write((byte)24);
                        writer.Write((ushort)n);
                    }
                    else if (n > 65535 && n <= uint.MaxValue)
                    {
                        writer.Write((byte)25);
                        writer.Write((uint)n);
                    }
                    else
                    {
                        writer.Write((byte)26);
                        writer.WriteUInt64((ulong)arg);
                    }
                    
                }
                else if (arg is float)
                {
                    writer.Write((byte)27);
                    writer.WriteSingle((float)arg);
                }
                else if (arg is double)
                {
                    writer.Write((byte)28);
                    writer.WriteDouble((double)arg);
                }
                else if (arg is bool)
                {
                    writer.WriteByte((bool)arg ? (byte)254 : (byte)255);
                }
                else if (arg is string)
                {
                    string str = (string)arg;
                    byte[] data = Encoding.UTF8.GetBytes(str);

                    if (data.Length <= byte.MaxValue)
                    {
                        writer.Write((byte)30);
                        writer.Write((byte)data.Length);
                    }
                    else if (data.Length <= ushort.MaxValue)
                    {
                        writer.Write((byte)31);
                        writer.Write((ushort)data.Length);
                    }
                    else
                    {
                        writer.Write((byte)32);
                        writer.Write((int)data.Length);
                    }
                    writer.Write(data);
                }
                else if (arg is byte[])
                {
                    var data = (byte[])arg;
                    if (data.Length <= byte.MaxValue)
                    {
                        writer.Write((byte)33);
                        writer.Write((ushort)data.Length);
                    }
                    else if (data.Length <= ushort.MaxValue)
                    {
                        writer.Write((byte)34);
                        writer.Write((ushort)data.Length);
                    }
                    else
                    {
                        writer.Write((byte)35);
                        writer.Write((int)data.Length);
                    }

                    writer.Write(data);
                }

                //17.数组类型
                else if (arg is Array)
                {
                    Log.Information("识别类型：Array");
                    //36:数组长度小于255
                    //37:数组长度小于65535
                    //38:数组长度更大
                    var arr = (Array)arg;
                    if (arr.Length <= 255)
                    {
                        writer.Write((byte)36);
                        writer.Write((byte)arr.Length);
                    }
                    else if (arr.Length <= 65535)
                    {
                        writer.Write((byte)37);
                        writer.Write((ushort)arr.Length);
                    }
                    else
                    {
                        writer.Write((byte)38);
                        writer.Write((int)arr.Length);
                    }

                    foreach (var item in arr)
                    {
                        SerializeData(writer, item);
                    }
                }

                //Dictionary<K,V>
                else if (arg is IDictionary)
                {
                    //39,40，41
                    var dict = (IDictionary)arg;
                    if (dict.Count <= 255)
                    {
                        writer.Write((byte)39);
                        writer.Write((byte)dict.Count);
                    }
                    else if(dict.Count <= ushort.MaxValue)
                    {
                        writer.Write((byte)40);
                        writer.Write((ushort)dict.Count);
                    }
                    else
                    {
                        writer.Write((byte)41);
                        writer.Write((int)dict.Count);
                    }

                    foreach (var key in dict.Keys)
                    {
                        SerializeData(writer, key);
                        SerializeData(writer, dict[key]);
                    }
                }


                //42.Vector2
                else if (arg is Vector2)
                {
                    var v = (Vector2)arg;
                    writer.Write((byte)42);
                    writer.Write(v.x);
                    writer.Write(v.y);
                }
                //43.Vector3
                else if (arg is Vector3)
                {
                    var v = (Vector3)arg;
                    writer.Write((byte)43);
                    writer.Write(v.x);
                    writer.Write(v.y);
                    writer.Write(v.z);
                }


                else
                {
                    Log.Error("DataConvert.Deserialize无法处理的类型：{0}", arg.GetType());
                    throw new Exception("DataConvert.Deserialize无法处理的类型：" + arg.GetType());
                }
            }
        }

        public static object[] Deserialize(byte[] data)
        {
            using var stream = DataStream.Allocate(data);
            return DeserializeData(stream);
        }

        private static object[] DeserializeData(DataStream reader)
        {
            var result = new List<object>();

            while (reader.Position < reader.Length)
            {
                result.Add(ParseItem(reader));
            }

            return result.ToArray();
        }
        private static object ParseItem(DataStream reader)
        {
            var type = reader.ReadByte();

            switch (type)
            {
                case 0: // null
                    return null;

                case 1: // sbyte
                    return (sbyte)reader.ReadSByte();

                case 2: // byte
                    return (byte)reader.ReadByte();

                case 3: // short
                    return (short)reader.ReadByte();

                case 4: // negative short
                    return (short)-reader.ReadByte();

                case 5: // other short
                    return (short)reader.ReadInt16();

                case 6: // ushort
                    return (ushort)reader.ReadByte();

                case 7: // other ushort
                    return (ushort)reader.ReadUInt16();

                case 8: // int
                    return (int)reader.ReadByte();

                case 9: // ushort int
                    return (int)reader.ReadUInt16();

                case 10: // negative int
                    return (int)-reader.ReadByte();

                case 11: // negative ushort int
                    return (int)-reader.ReadUInt16();

                case 12: // other int
                    return (int)reader.ReadInt32();

                case 13: // uint
                    return (uint)reader.ReadByte();

                case 14: // ushort uint
                    return (uint)reader.ReadUInt16();

                case 15: // other uint
                    return (uint)reader.ReadUInt32();

                case 16: // long by byte
                    return (long)reader.ReadByte();

                case 17: // long by -byte
                    return (long)-reader.ReadByte();

                case 18: // long ushort
                    return (long)reader.ReadUInt16();

                case 19: // long by -ushort
                    return (long)-reader.ReadUInt16();

                case 20: // long by uint
                    return (long)reader.ReadUInt32();

                case 21: // long by -uint
                    return (long)-reader.ReadUInt32();

                case 22: // long
                    return (long)reader.ReadInt64();

                case 23: // ulong by byte
                    return (ulong)reader.ReadByte();

                case 24: // ulong by ushort
                    return (ulong)reader.ReadUInt16();

                case 25: // ulong by uint
                    return (ulong)reader.ReadUInt32();

                case 26: // ulong
                    return (ulong)reader.ReadUInt64();

                case 27: // float
                    return (float)reader.ReadSingle();

                case 28: // double
                    return (double)reader.ReadDouble();

                case 254: // true
                    return true;

                case 255: // false
                    return false;

                case 30: // string (byte length)
                    var strLen = (int)reader.ReadByte();
                    var strBytes = reader.ReadBytes(strLen);
                    return Encoding.UTF8.GetString(strBytes);

                case 31: // string (ushort length)
                    var strLen2 = (int)reader.ReadUInt16();
                    var strBytes2 = reader.ReadBytes(strLen2);
                    return Encoding.UTF8.GetString(strBytes2);

                case 32: // string (int length)
                    var strLen3 = reader.ReadInt32();
                    var strBytes3 = reader.ReadBytes(strLen3);
                    return Encoding.UTF8.GetString(strBytes3);

                case 33: // byte array (byte length)
                    var arrLen = reader.ReadByte();
                    return reader.ReadBytes(arrLen);

                case 34: // byte array (ushort length)
                    var arrLen2 = reader.ReadUInt16();
                    return reader.ReadBytes(arrLen2);

                case 35: // byte array (int length)
                    var arrLen3 = reader.ReadInt32();
                    return reader.ReadBytes(arrLen3);

                //对象数组类型
                case 36: // array (byte length)
                    {
                        int len = reader.ReadByte();
                        var arr = new object[len];
                        for (int i = 0; i < len; i++)
                        {
                            arr[i] = ParseItem(reader);
                        }
                        return arr;
                    }


                case 37: // array (ushort length)
                    {
                        var len = reader.ReadUInt16();
                        var arr = new object[len];
                        for (int i = 0; i < len; i++)
                        {
                            arr[i] = ParseItem(reader);
                        }
                        return arr;
                    }

                case 38: // array (int length)
                    {
                        var len = reader.ReadUInt32();
                        var arr = new object[len];
                        for (int i = 0; i < len; i++)
                        {
                            arr[i] = ParseItem(reader);
                        }
                        return arr;
                    }

                case 39: // dictionary (byte length)
                    var dictLen = reader.ReadByte();
                    var dict = new Dictionary<object, object>();
                    for (int i = 0; i < dictLen; i++)
                    {
                        var key = ParseItem(reader);
                        var value = ParseItem(reader);
                        dict.Add(key, value);
                    }
                    return dict;

                case 40: // dictionary (ushort length)
                    var dictLen2 = reader.ReadUInt16();
                    var dict2 = new Dictionary<object, object>();
                    for (int i = 0; i < dictLen2; i++)
                    {
                        var key = ParseItem(reader);
                        var value = ParseItem(reader);
                        dict2.Add(key, value);
                    }
                    return dict2;

                case 41: // dictionary (int length)
                    var dictLen3 = reader.ReadInt32();
                    var dict3 = new Dictionary<object, object>();
                    for (int i = 0; i < dictLen3; i++)
                    {
                        var key = ParseItem(reader);
                        var value = ParseItem(reader);
                        dict3.Add(key, value);
                    }
                    return dict3;


                case 42: // Vector2
                    return new Vector2(reader.ReadSingle(), reader.ReadSingle());

                case 43: // Vector3
                    return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
              

                default: break;
            }
            Log.Error("DataConvert.Deserialize无法处理的类型：{0}", type);
            throw new Exception("DataConvert.Deserialize无法处理的类型：" + type);
        }



    }

}
