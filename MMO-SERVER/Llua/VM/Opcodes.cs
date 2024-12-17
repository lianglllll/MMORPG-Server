namespace lLua.VM
{
    // 操作码format
    public enum OPCODE_FORMAT
    {
        IABC,       // 携带3个参数：8，9，9    
        IABx,       // 携带2个参数：8，18
        IAsBx,      // 携带2个参数：8，18  第二个参数解释成有符号整数
        IAx         // 携带一个参数：26
    }

    // 操作码类型
    public enum OPCODE_TYPE
    {
        OP_MOVE,      // 0
        OP_LOADK,     // 1
        OP_LOADKX,    // 2
        OP_LOADBOOL,  // 3
        OP_LOADNIL,   // 4
        OP_GETUPVAL,  // 5
        OP_GETTABUP,  // 6
        OP_GETTABLE,  // 7
        OP_SETTABUP,  // 8
        OP_SETUPVAL,  // 9
        OP_SETTABLE,  // 10
        OP_NEWTABLE,  // 11
        OP_SELF,      // 12
        OP_ADD,       // 13
        OP_SUB,       // 14
        OP_MUL,       // 15
        OP_MOD,       // 16
        OP_POW,       // 17
        OP_DIV,       // 18
        OP_IDIV,      // 19
        OP_BAND,      // 20
        OP_BOR,       // 21
        OP_BXOR,      // 22
        OP_SHL,       // 23
        OP_SHR,       // 24
        OP_UNM,       // 25
        OP_BNOT,      // 26
        OP_NOT,       // 27
        OP_LEN,       // 28
        OP_CONCAT,    // 29
        OP_JMP,       // 30
        OP_EQ,        // 31
        OP_LT,        // 32
        OP_LE,        // 33
        OP_TEST,      // 34
        OP_TESTSET,   // 35
        OP_CALL,      // 36
        OP_TAILCALL,  // 37
        OP_RETURN,    // 38
        OP_FORLOOP,   // 39
        OP_FORPREP,   // 40
        OP_TFORCALL,  // 41
        OP_TFORLOOP,  // 42
        OP_SETLIST,   // 43
        OP_CLOSURE,   // 44
        OP_VARARG,    // 45
        OP_EXTRAARG   // 46
    }

    // 操作数类型
    public enum OPARG_TYPE
    {
        OpArgN, // 不使用
        OpArgU, // 布尔值、整数值、upvalue索引、子函数索引
        OpArgR, // iABC:寄存器索引 iAsBx:跳转偏移   
        OpArgK  // iABX:常量表索引  iABC: 寄存器索引||常量表索引  
    }

    public class Opcode
    {
        public byte TestFlag;   // operator is a test (next instruction must be a jump)
        public byte SetAFlag;   // instruction set register A
        public byte ArgBMode;   // B arg mode
        public byte ArgCMode;   // C arg mode
        public byte OpMode;     // op mode
        public string Name;     // name of the opcode

        public Opcode(byte testFlag, byte setAFlag, byte argBMode, byte argCMode, byte opMode, string name)
        {
            TestFlag = testFlag;
            SetAFlag = setAFlag;
            ArgBMode = argBMode;
            ArgCMode = argCMode;
            OpMode = opMode;
            Name = name;
        }
    }

    public class Opcodes
    {
        // 定义指令列表
        public static List<Opcode> opcodes = new List<Opcode>
        {
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "MOVE"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABx, "LOADK"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgN, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABx, "LOADKX"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "LOADBOOL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "LOADNIL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "GETUPVAL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "GETTABUP"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "GETTABLE"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "SETTABUP"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "SETUPVAL"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "SETTABLE"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "NEWTABLE"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "SELF"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "ADD"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "SUB"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "MUL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "MOD"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "POW"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "DIV"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "IDIV"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "BAND"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "BOR"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "BXOR"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "SHL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "SHR"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "UNM"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "BNOT"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "NOT"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "LEN"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgR, (byte)OPCODE_FORMAT.IABC, "CONCAT"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IAsBx, "JMP"),
            new Opcode(1, 0, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "EQ"),
            new Opcode(1, 0, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "LT"),
            new Opcode(1, 0, (byte)OPARG_TYPE.OpArgK, (byte)OPARG_TYPE.OpArgK, (byte)OPCODE_FORMAT.IABC, "LE"),
            new Opcode(1, 0, (byte)OPARG_TYPE.OpArgN, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "TEST"),
            new Opcode(1, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "TESTSET"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "CALL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "TAILCALL"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "RETURN"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IAsBx, "FORLOOP"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IAsBx, "FORPREP"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgN, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "TFORCALL"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgR, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IAsBx, "TFORLOOP"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IABC, "SETLIST"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABx, "CLOSURE"),
            new Opcode(0, 1, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgN, (byte)OPCODE_FORMAT.IABC, "VARARG"),
            new Opcode(0, 0, (byte)OPARG_TYPE.OpArgU, (byte)OPARG_TYPE.OpArgU, (byte)OPCODE_FORMAT.IAx, "EXTRAARG")
        };
    }

}
