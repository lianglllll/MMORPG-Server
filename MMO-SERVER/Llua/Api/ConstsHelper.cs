using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lLua.Api
{
    public enum LuaDataType
    {
        LUA_TNONE = -1,
        LUA_TNIL,
        LUA_TBOOLEAN,
        LUA_TLIGHTUSERDATA,
        LUA_TNUMBER,
        LUA_TSTRING,
        LUA_TTABLE,
        LUA_TFUNCTION,
        LUA_TUSERDATA,
        LUA_TTHREAD
    }

    public class ConstsHelper
    {
    }
}
