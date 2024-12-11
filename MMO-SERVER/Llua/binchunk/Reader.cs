using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace lLua.binchunk
{
    //具体的而记者chunk解析工作由reader来完成
    public class Reader
    {
        private byte[] data;
        private int position;
        public Reader(byte[] data)
        {
            this.data = data;
            this.position = 0;
        }

        public byte ReadByte()
        {
            if (position + 1 > data.Length)
            {
                throw new InvalidOperationException("Attempt to read beyond the end of the data.");
            }

            byte b = data[position];
            position++;
            return b;
        }

        public uint ReadUint32()
        {
            if (position + 4 > data.Length)
            {
                throw new InvalidOperationException("Attempt to read beyond the end of the data.");
            }

            uint value = BitConverter.ToUInt32(data, position);
            position += 4;
            return value;
        }

        public ulong ReadUint64()
        {
            if (position + 8 > data.Length)
            {
                throw new InvalidOperationException("Attempt to read beyond the end of the data.");
            }

            ulong value = BitConverter.ToUInt64(data, position);
            position += 8;
            return value;
        }

        public long ReadLuaInteger()
        {
            return (long)ReadUint64();
        }

        public double ReadLuaNumber()
        {
            ulong bits = ReadUint64();
            return BitConverter.Int64BitsToDouble((long)bits);
        }

        public byte[] ReadBytes(uint n)
        {
            if (position + n > data.Length)
            {
                throw new InvalidOperationException("Attempt to read beyond the end of the data.");
            }

            byte[] bytes = new byte[n];
            Array.Copy(data, position, bytes, 0, n);
            position += (int)n;
            return bytes;
        }

        public string ReadString()
        {
            uint size = ReadByte(); // Attempt to read a short string

            if (size == 0) // Null string
            {
                return "";
            }

            if (size == 0xFF) // Long string
            {
                size = (uint)ReadUint64();
            }

            byte[] bytes = ReadBytes(size - 1); // Read the string bytes, excluding the terminator
            return Encoding.UTF8.GetString(bytes); // Convert bytes to UTF-8 string
        }

        public void CheckHeader()
        {
            if (Encoding.ASCII.GetString(ReadBytes(4)) != LuaConstants.LUA_SIGNATURE)
            {
                throw new InvalidOperationException("not a precompiled chunk!");
            }
            else if (ReadByte() != LuaConstants.LUAC_VERSION)
            {
                throw new InvalidOperationException("version mismatch!");
            }
            else if (ReadByte() != LuaConstants.LUAC_FORMAT)
            {
                throw new InvalidOperationException("format mismatch!");
            }
            else if (!ReadBytes(6).SequenceEqual(LuaConstants.LUAC_DATA))
            {
                throw new InvalidOperationException("corrupted!");
            }
            else if (ReadByte() != LuaConstants.CINT_SIZE)
            {
                throw new InvalidOperationException("int size mismatch!");
            }
            else if (ReadByte() != LuaConstants.CSIZET_SIZE)
            {
                throw new InvalidOperationException("size_t size mismatch!");
            }
            else if (ReadByte() != LuaConstants.INSTRUCTION_SIZE)
            {
                throw new InvalidOperationException("instruction size mismatch!");
            }
            else if (ReadByte() != LuaConstants.LUA_INTEGER_SIZE)
            {
                throw new InvalidOperationException("lua_Integer size mismatch!");
            }
            else if (ReadByte() != LuaConstants.LUA_NUMBER_SIZE)
            {
                throw new InvalidOperationException("lua_Number size mismatch!");
            }
            else if (ReadLuaInteger() != LuaConstants.LUAC_INT)
            {
                throw new InvalidOperationException("endianness mismatch!");
            }
            else if (ReadLuaNumber() != LuaConstants.LUAC_NUM)
            {
                throw new InvalidOperationException("float format mismatch!");
            }
        }

        public Prototype ReadProto(string parentSource)
        {
            string source = ReadString();
            if (string.IsNullOrEmpty(source))
            {
                source = parentSource;
            }

            return new Prototype
            {
                Source = source,
                LineDefined = ReadUint32(),
                LastLineDefined = ReadUint32(),
                NumParams = ReadByte(),
                IsVararg = ReadByte(),
                MaxStackSize = ReadByte(),
                Code = ReadCode(),
                Constants = ReadConstants(),
                Upvalues = ReadUpvalues(),
                Protos = ReadProtos(source),
                LineInfo = ReadLineInfo(),
                LocVars = ReadLocVars(),
                UpvalueNames = ReadUpvalueNames()
            };
        }

        public List<uint> ReadCode()
        {
            // 创建一个大小为 ReadUint32 返回值的 uint 列表
            int size = (int)ReadUint32();  // 从字节流中读取长度
            List<uint> code = new List<uint>(size);

            for (int i = 0; i < size; i++)
            {
                // 逐个读取 uint 值并加入列表
                code.Add(ReadUint32());
            }

            return code;
        }

        public List<object> ReadConstants()
        {
            // 创建一个大小为 ReadUint32 返回值的 object 列表
            int size = (int)ReadUint32();  // 从字节流中读取常量表的长度
            List<object> constants = new List<object>(size);

            for (int i = 0; i < size; i++)
            {
                // 读取常量并加入列表
                constants.Add(ReadConstant());
            }

            return constants;
        }

        public object ReadConstant()
        {
            byte tag = ReadByte(); // 获取标签

            switch (tag)
            {
                case TagType.Nil:
                    return null; // 返回 null

                case TagType.Boolean:
                    return ReadByte() != 0; // 返回布尔值

                case TagType.Integer:
                    return ReadLuaInteger(); // 返回整数

                case TagType.Number:
                    return ReadLuaNumber(); // 返回浮点数

                case TagType.ShortStr:
                case TagType.LongStr:
                    return ReadString(); // 返回字符串

                default:
                    throw new InvalidOperationException("Corrupted!"); // 抛出异常
            }
        }

        public List<Upvalue> ReadUpvalues()
        {
            uint count = ReadUint32();
            var upvalues = new List<Upvalue>((int)count);

            for (int i = 0; i < count; i++)
            {
                byte instack = ReadByte();
                byte idx = ReadByte();
                upvalues.Add(new Upvalue(instack, idx));
            }
            return upvalues;
        }

        public List<Prototype> ReadProtos(string parentSource)
        {
            uint count = ReadUint32();
            var protos = new List<Prototype>((int)count);

            for (int i = 0; i < count; i++)
            {
                protos.Add(ReadProto(parentSource));
            }

            return protos;
        }

        public List<uint> ReadLineInfo()
        {
            uint count = ReadUint32();
            var lineInfo = new List<uint>((int)count);

            for (int i = 0; i < count; i++)
            {
                lineInfo.Add(ReadUint32());
            }

            return lineInfo;
        }

        public List<LocVar> ReadLocVars()
        {
            uint count = ReadUint32();
            var locVars = new List<LocVar>((int)count);

            for (int i = 0; i < count; i++)
            {
                string varName = ReadString();
                uint startPC = ReadUint32();
                uint endPC = ReadUint32();
                locVars.Add(new LocVar(varName, startPC, endPC));
            }

            return locVars;
        }

        public List<string> ReadUpvalueNames()
        {
            uint count = ReadUint32();
            var names = new List<string>((int)count);

            for (int i = 0; i < count; i++)
            {
                names.Add(ReadString());
            }

            return names;
        }

    }
}
