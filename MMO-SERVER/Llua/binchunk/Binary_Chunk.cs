
using lLua.VM;

namespace lLua.Binchunk
{
    public static class LuaConstants
    {
        public const string LUA_SIGNATURE       = "\x1bLua";
        public const byte LUAC_VERSION          = 0x53;
        public const byte LUAC_FORMAT           = 0;
        public static readonly byte[] LUAC_DATA = { 0x19, 0x93, 0x0D, 0x0A, 0x1A, 0x0A };
        public const int CINT_SIZE              = 4;
        public const int CSIZET_SIZE            = 4; 
        public const int INSTRUCTION_SIZE       = 4;
        public const int LUA_INTEGER_SIZE       = 8;
        public const int LUA_NUMBER_SIZE        = 8;
        public const int LUAC_INT               = 0x5678;
        public const double LUAC_NUM            = 370.5;
    }

    public static class TagType
    {
        public const int Nil = 0x00;
        public const int Boolean = 0x01;
        public const int Number = 0x03;
        public const int Integer = 0x13;
        public const int ShortStr = 0x04;
        public const int LongStr = 0x14;
    }

    //头部字段
    public class Header
    {
        private uint signature;         //签名,不符合拒绝加载
        private byte version;           //版本号(大版本号Mv、小版本号mv、发布号，例如：5.3.4。其值为Mv*16+mv)，不符合拒绝加载
        private byte format;            //格式号(0)，不符合拒绝加载
        private byte[] luacData;        //进一步校验，不符合拒绝加载
        private byte cintSize;          //04
        private byte sizetSize;         //08
        private byte instructonSize;    //04
        private byte luaIntegerSize;    //08
        private byte luaNumberSize;     //08
        private byte[] luacInt;         //n个字节存放lua整数值0x5678，所以这里n是8。目的是检查大小端
        private byte[] luacNum;         //最后n个字节存放Lua浮点数370.5，所以这里n是8.目的是检测二进制chunk所使用的浮点数格式
    }

    //函数原型
    public class Prototype
    {
        public string Source;               //源文件
        public uint LineDefined;            //起止行号
        public uint LastLineDefined;
        public byte NumParams;              //固定参数个数
        public byte IsVararg;               //是否是Vararg函数
        public byte MaxStackSize;           //寄存器数量
        public List<uint> Code;             //指令表
        public List<object> Constants;      //常量表
        public List<Upvalue> Upvalues;      //Upvalue表
        public List<Prototype> Protos;      //子函数原型表
        public List<uint> LineInfo;         //行号表
        public List<LocVar> LocVars;        //局部变量表
        public List<string> UpvalueNames;   //Upvalue名列表
    }

    public class Upvalue
    {
        public byte istack;
        public byte idx;

        public Upvalue(byte istack, byte idx)
        {
            this.istack = istack;
            this.idx = idx;
        }
    }

    public class LocVar
    {
        public string varName;
        public uint startPC;
        public uint endPC;

        public LocVar(string varName, uint startPC, uint endPC)
        {
            this.varName = varName;
            this.startPC = startPC;
            this.endPC = endPC;
        }
    }

    public class Binary_Chunk
    {
        private Header header;      // 头部
        private int upvaluesSize;   // 主函数upvalue数量
        private Prototype mainFunc; // 主函数原型

        public static Prototype Undump(byte[] data)
        {
            var reader = new Reader(data);
            reader.CheckHeader();           // 校验头部
            reader.ReadByte();              // 跳过 Upvalue 数量
            return reader.ReadProto("");    // 读取函数原型
        }

        public static string UpvalName(Prototype f, int idx)
        {
            if (f.UpvalueNames != null && f.UpvalueNames.Count > 0)
            {
                return f.UpvalueNames[idx];
            }
            return "-";
        }

        public static string ConstantToString(object k)
        {
            switch (k)
            {
                case null:
                    return "nil";
                case bool b:
                    return b.ToString().ToLower();
                case double d:
                    return d.ToString("G");
                case long i:
                    return i.ToString();
                case string s:
                    return $"\"{s}\"";
                default:
                    return "?";
            }
        }

        public static void Print(Prototype f)
        {
            // 头部信息
            string funcType = (f.LineDefined > 0) ? "function" : "main";
            string varargFlag = (f.IsVararg > 0) ? "+" : "";

            Console.WriteLine("\n{0} <{1}:{2}, {3}> ({4} instructions)",
                funcType, f.Source, f.LineDefined, f.LastLineDefined, f.Code.Count);

            Console.WriteLine("{0}{1} params, {2} slots, {3} upvalues, ",
                f.NumParams, varargFlag, f.MaxStackSize, f.Upvalues.Count);

            Console.WriteLine("{0} locals, {1} constants, {2} functions",
                f.LocVars.Count, f.Constants.Count, f.Protos.Count);

            // code
            for (int pc = 0; pc < f.Code.Count; pc++)
            {
                var c = f.Code[pc];
                string line = "-";
                if (f.LineInfo.Count > 0)
                {
                    line = f.LineInfo[pc].ToString();
                }

                var i = new Instruction(c);
                Console.Write($"\t{pc + 1}\t[{line}]\t{i.OpName()} \t");
                PrintOperands(i);
                Console.WriteLine();
            }

            // 常量表
            Console.WriteLine("constants ({0}):", f.Constants.Count);
            for (int i = 0; i < f.Constants.Count; i++)
            {
                Console.WriteLine("\t{0}\t{1}", i + 1, ConstantToString(f.Constants[i]));
            }

            //局部变量
            Console.WriteLine("locals ({0}):", f.LocVars.Count);
            for (int i = 0; i < f.LocVars.Count; i++)
            {
                var locVar = f.LocVars[i];
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", i, locVar.varName, locVar.startPC + 1, locVar.startPC + 1);
            }

            // upvalue
            Console.WriteLine("upvalues ({0}):", f.Upvalues.Count);
            for (int i = 0; i < f.Upvalues.Count; i++)
            {
                var upval = f.Upvalues[i];
                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", i, UpvalName(f, i), upval.istack, upval.idx);
            }
        }

        public static void PrintOperands(Instruction i)
        {
            switch (i.OpMode())
            {
                case (byte)OPCODE_FORMAT.IABC:
                    var (a, b, c) = i.ABC();
                    Console.Write(a);
                    if (i.ArgBMode() != (byte)OPARG_TYPE.OpArgN)
                    {
                        Console.Write(b > 0xFF ? $" {-1 - (b & 0xFF)}" : $" {b}");
                    }
                    if (i.ArgCMode() != (byte)OPARG_TYPE.OpArgN)
                    {
                        Console.Write(c > 0xFF ? $" {-1 - (c & 0xFF)}" : $" {c}");
                    }
                    break;

                case (byte)OPCODE_FORMAT.IABx:
                    var (a_abx, bx) = i.ABx();
                    Console.Write(a_abx);
                    if (i.ArgBMode() == (byte)OPARG_TYPE.OpArgK)
                    {
                        Console.Write($" {-1 - bx}");
                    }
                    else if (i.ArgBMode() == (byte)OPARG_TYPE.OpArgU)
                    {
                        Console.Write($" {bx}");
                    }
                    break;

                case (byte)OPCODE_FORMAT.IAsBx:
                    var (a_asbx, sbx) = i.AsBx();
                    Console.Write($"{a_asbx} {sbx}");
                    break;

                case (byte)OPCODE_FORMAT.IAx:
                    var ax = i.Ax();
                    Console.Write($" {-1 - ax}");
                    break;
            }
        }
    }
}
