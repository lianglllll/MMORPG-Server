using lLua.Api;


namespace lLua.State
{
    public class LuaValue
    {
        public object Value { get; set; }
        public LuaValue() { }
        public LuaValue(object value)
        {
            Value = value;
        }
    }

    public class LuaDataTypeHelper
    {
        public static LuaDataType TypeOf(object val)
        {
            return val switch
            {
                null => LuaDataType.LUA_TNIL,
                bool => LuaDataType.LUA_TBOOLEAN,
                int => LuaDataType.LUA_TNUMBER,
                long => LuaDataType.LUA_TNUMBER,
                float => LuaDataType.LUA_TNUMBER,
                double => LuaDataType.LUA_TNUMBER,
                string => LuaDataType.LUA_TSTRING,
                _ => throw new NotImplementedException("Unhandled type!")
            };
        }
    }


}
