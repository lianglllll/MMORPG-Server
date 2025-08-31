using Serilog;
using System;
using System.Collections.Generic;

namespace Common.Summer.StaticData
{
    public static class ErrorCode
    {
        private static Dictionary<string, int> _codeMap     = new();
        private static Dictionary<string, string> _descMap  = new();
        private static readonly string jsonFilePath         = "ErrorCodeDefine.json";

        static ErrorCode()
        {
            try
            {
                var errorCodes = StaticDataLoader.LoadByFilePath1<ErrorCodeDefine>(jsonFilePath).Values;

                _codeMap.Clear();
                _descMap.Clear();

                foreach (var item in errorCodes)
                {
                    _codeMap[item.SID] = item.ID;
                    _descMap[item.SID] = item.Desc;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"加载错误码失败: {ex.Message}");
                throw;
            }
        }

        public static int GetCode(string id)
        {
            if (_codeMap.TryGetValue(id, out int code))
            {
                return code;
            }

            throw new KeyNotFoundException($"未找到错误码标识符: {id}");
        }

        public static string GetDescription(string code)
        {
            if (_descMap.TryGetValue(code, out string desc))
            {
                return desc;
            }

            return "未知错误码";
        }
    }
}
