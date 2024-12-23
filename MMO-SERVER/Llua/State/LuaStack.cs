using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lLua.State
{
    public class LuaStack
    {
        private List<LuaValue> slots;
        private int top;

        public LuaStack(int size)
        {
            slots = new List<LuaValue>(new LuaValue[size]);
            top = 0;
        }

        public void Push(LuaValue val)
        {
            if (top == slots.Count)
            {
                throw new InvalidOperationException("stack overflow!");
            }
            slots[top] = val;
            top++;
        }

        public LuaValue Pop()
        {
            if (top < 1)
            {
                throw new InvalidOperationException("stack underflow!");
            }
            top--;
            var val = slots[top];
            slots[top] = null; // 将槽位设置为 null，释放引用。
            return val;
        }

        public int AbsIndex(int idx)
        {
            if (idx >= 0)
            {
                return idx;
            }
            return idx + top + 1;
        }

        public bool IsValid(int idx)
        {
            int absIdx = AbsIndex(idx);
            return absIdx > 0 && absIdx <= top;
        }

        public LuaValue Get(int idx)
        {
            int absIdx = AbsIndex(idx);
            if (absIdx > 0 && absIdx <= top)
            {
                return slots[absIdx - 1];
            }
            return null;
        }

        public void Set(int idx, LuaValue val)
        {
            int absIdx = AbsIndex(idx);
            if (absIdx > 0 && absIdx <= top)
            {
                slots[absIdx - 1] = val;
                return;
            }
            throw new InvalidOperationException("Invalid index!");
        }
    }
}
