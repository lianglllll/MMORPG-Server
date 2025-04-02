using System.Collections.Generic;
using Google.Protobuf;
using System;
using Google.Protobuf.Reflection;
using Serilog;
using Common.Summer.Tools;
using System.Buffers.Binary;

namespace Common.Summer.Net
{
    /// <summary>
    /// Protobuf序列化与反序列化
    /// </summary>
    public class ProtoHelper : Singleton<ProtoHelper>
    {
        //考虑到每次输送类型名太长了，所以imessage类型一个序号
        private static Dictionary<int, Type> m_sequence2type = new Dictionary<int, Type>();
        private static Dictionary<Type, int> m_type2sequence = new Dictionary<Type, int>();

        public void Init()
        {

        }
        public void UnInit()
        {

        }
        public bool Register<T>(int id) where T : IMessage
        {
            Type type = typeof(T);
            m_sequence2type[id] = type;
            m_type2sequence[type] = id;
            return true;
        }
        public int Type2Seq(Type type)
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
        public Type Seq2Type(int code)
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
        public IMessage ByteArrayParse2IMessage(ReadOnlyMemory<byte> data)
        {
            /*            ushort typeCode = _GetUShort(data, 0);
                        Type t = Seq2Type(typeCode);
                        if (t == null)
                        {
                            Log.Error($"[ProtoHelper.ParseFrom]解析失败，协议号:{typeCode}");
                            return null;
                        }
                        var desc = t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;
                        var msg = desc.Parser.ParseFrom(data, 2, data.Length - 2);
                        return msg;*/

            // 直接操作内存，无需复制
            ReadOnlySpan<byte> span = data.Span;
            ushort typeCode = BinaryPrimitives.ReadUInt16BigEndian(span);

            Type t = Seq2Type(typeCode);
            var desc = t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;

            // 使用Span解析
            return desc.Parser.ParseFrom(span.Slice(2));
        }
        public byte[] IMessageParse2ByteArray(IMessage message)
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

        private ushort _GetUShort(byte[] data, int offset)
        {
            if (BitConverter.IsLittleEndian)
                return (ushort)(data[offset] << 8 | data[offset + 1]);
            return (ushort)(data[offset + 1] << 8 | data[offset]);
        }
    }
}
