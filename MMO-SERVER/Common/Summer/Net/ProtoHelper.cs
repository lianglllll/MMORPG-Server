using System.Collections.Generic;
using Google.Protobuf;
using System;
using Google.Protobuf.Reflection;
using Serilog;
using Common.Summer.Tools;

namespace Common.Summer.Net
{
    /// <summary>
    /// Protobuf序列化与反序列化
    /// </summary>
    public class ProtoHelper
    {
        //考虑到每次输送类型名太长了，所以imessage类型一个序号
        private static Dictionary<int, Type> m_sequence2type = new Dictionary<int, Type>();
        private static Dictionary<Type, int> m_type2sequence = new Dictionary<Type, int>();

        public static void Init()
        {

        }
        public static void UnInit()
        {

        }
        public static bool Register<T>(int id) where T : IMessage
        {
            Type type = typeof(T);
            m_sequence2type[id] = type;
            m_type2sequence[type] = id;
            return true;
        }
        public static int Type2Seq(Type type)
        {
            if (m_type2sequence.ContainsKey(type))
            {
                return m_type2sequence[type];
            }
            else
            {
                Log.Error($"[ProtoHelper.Type2Seq]未找到对应的协议类型:{type.ToString()}");
                return -1;
            }
        }
        public static Type Seq2Type(int code)
        {
            if (m_sequence2type.ContainsKey(code))
            {
                return m_sequence2type[code];
            }
            else
            {
                Log.Error($"[ProtoHelper.Seq2Type]未找到对应的协议id:{code}");
                return null;
            }
        }
        public static IMessage BytesParse2IMessage(byte[] data)
        {
            ushort typeCode = _GetUShort(data, 0);
            Type t = Seq2Type(typeCode);
            if (t == null)
            {
                Log.Error($"[ProtoHelper.ParseFrom]解析失败，协议号:{typeCode}");
                return null;
            }
            var desc = t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;
            var msg = desc.Parser.ParseFrom(data, 2, data.Length - 2);
            return msg;
        }
        public static byte[] IMessageParse2Bytes(IMessage message)
        {
            //获取imessage类型所对应的编号，网络传输我们只传输编号
            using (var ds = DataStream.Allocate())
            {
                int code = Type2Seq(message.GetType());
                if (code == -1)
                {
                    return null;
                }
                ds.WriteInt(message.CalculateSize() + 2);           //长度字段
                ds.WriteUShort((ushort)code);                       //协议编号字段
                message.WriteTo(ds);                                //数据
                return ds.ToArray();
            }
        }
        private static ushort _GetUShort(byte[] data, int offset)
        {
            if (BitConverter.IsLittleEndian)
                return (ushort)(data[offset] << 8 | data[offset + 1]);
            return (ushort)(data[offset + 1] << 8 | data[offset]);
        }
    }
}
