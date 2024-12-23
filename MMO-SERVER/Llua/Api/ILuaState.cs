using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lLua.Api
{
    public interface ILuaState
    {
        /* Basic stack manipulation */
        int GetTop();
        int AbsIndex(int idx);
        bool CheckStack(int n);
        void Pop(int n);
        void Copy(int fromIdx, int toIdx);
        void PushValue(int idx);
        void Replace(int idx);
        void Insert(int idx);
        void Remove(int idx);
        void Rotate(int idx, int n);
        void SetTop(int idx);

        /* Access functions (stack -> c#) */
        string TypeName(LuaDataType tp);
        LuaDataType Type(int idx);
        bool IsNone(int idx);
        bool IsNil(int idx);
        bool IsNoneOrNil(int idx);
        bool IsBoolean(int idx);
        bool IsInteger(int idx);
        bool IsNumber(int idx);
        bool IsString(int idx);
        bool ToBoolean(int idx);
        long ToInteger(int idx);
        (long value, bool isValid) ToIntegerX(int idx);  // Tuple for multi-value return
        double ToNumber(int idx);
        (double value, bool isValid) ToNumberX(int idx); // Tuple for multi-value return
        string ToString(int idx);
        (string value, bool isValid) ToStringX(int idx); // Tuple for multi-value return

        /* Push functions (c# -> stack) */
        void PushNil();
        void PushBoolean(bool b);
        void PushInteger(long n);
        void PushNumber(double n);
        void PushString(string s);
    }
}
