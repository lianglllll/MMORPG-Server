namespace lLua.VM
{
    public class LInstruction
    {
        private readonly uint instructionValue;
        private const int MAXARG_Bx = 1 << 18 - 1;        // 2^18-1 = 262143
        private const int MAXARG_sBx = MAXARG_Bx >> 1;

        public LInstruction(uint value)
        {
            instructionValue = value;
        }

        private int Opcode()
        {
            //取低6位
            return (int)instructionValue & 0x3F;
        }

        public (int a, int b, int c) ABC()
        {
            int a = (int)((instructionValue >> 6) & 0xFF);
            int c = (int)((instructionValue >> 14) & 0x1FF);
            int b = (int)((instructionValue >> 23) & 0x1FF);
            return (a, b, c);
        }

        public (int a, int bx) ABx()
        {
            int a = (int)((instructionValue >> 6) & 0xFF);
            int bx = (int)(instructionValue >> 14);
            return (a, bx);
        }

        public (int a, int sbx) AsBx()
        {
            var (a, bx) = ABx();
            int sbx = (int)bx - MAXARG_sBx;
            return (a, sbx);
        }

        public int Ax()
        {
            return (int)(instructionValue >> 6);
        }

        public string OpName()
        {
            return Opcodes.opcodes[Opcode()].Name;
        }

        public byte OpMode()
        {
            return Opcodes.opcodes[Opcode()].OpMode;
        }

        public byte ArgBMode()
        {
            return Opcodes.opcodes[Opcode()].ArgBMode;
        }

        public byte ArgCMode()
        {
            return Opcodes.opcodes[Opcode()].ArgCMode;
        }
    }
}
