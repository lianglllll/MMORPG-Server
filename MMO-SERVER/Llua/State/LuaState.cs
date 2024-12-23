using lLua.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lLua.State
{
    public class LuaState : ILuaState
    {
        private LuaStack luaStack;

        public LuaState()
        {
            luaStack = new LuaStack(20);
        }

        public int AbsIndex(int idx)
        {
            throw new NotImplementedException();
        }

        public bool CheckStack(int n)
        {
            throw new NotImplementedException();
        }

        public void Copy(int fromIdx, int toIdx)
        {
            throw new NotImplementedException();
        }

        public int GetTop()
        {
            throw new NotImplementedException();
        }

        public void Insert(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsBoolean(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsInteger(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsNil(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsNone(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsNoneOrNil(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsNumber(int idx)
        {
            throw new NotImplementedException();
        }

        public bool IsString(int idx)
        {
            throw new NotImplementedException();
        }

        public void Pop(int n)
        {
            throw new NotImplementedException();
        }

        public void PushBoolean(bool b)
        {
            throw new NotImplementedException();
        }

        public void PushInteger(long n)
        {
            throw new NotImplementedException();
        }

        public void PushNil()
        {
            throw new NotImplementedException();
        }

        public void PushNumber(double n)
        {
            throw new NotImplementedException();
        }

        public void PushString(string s)
        {
            throw new NotImplementedException();
        }

        public void PushValue(int idx)
        {
            throw new NotImplementedException();
        }

        public void Remove(int idx)
        {
            throw new NotImplementedException();
        }

        public void Replace(int idx)
        {
            throw new NotImplementedException();
        }

        public void Rotate(int idx, int n)
        {
            throw new NotImplementedException();
        }

        public void SetTop(int idx)
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean(int idx)
        {
            throw new NotImplementedException();
        }

        public long ToInteger(int idx)
        {
            throw new NotImplementedException();
        }

        public (long value, bool isValid) ToIntegerX(int idx)
        {
            throw new NotImplementedException();
        }

        public double ToNumber(int idx)
        {
            throw new NotImplementedException();
        }

        public (double value, bool isValid) ToNumberX(int idx)
        {
            throw new NotImplementedException();
        }

        public string ToString(int idx)
        {
            throw new NotImplementedException();
        }

        public (string value, bool isValid) ToStringX(int idx)
        {
            throw new NotImplementedException();
        }

        public LuaDataType Type(int idx)
        {
            throw new NotImplementedException();
        }

        public string TypeName(LuaDataType tp)
        {
            throw new NotImplementedException();
        }
    }
}
