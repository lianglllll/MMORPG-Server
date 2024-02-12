using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable



/// <summary>
/// 弃用
/// </summary>
namespace Common
{
   
    #region 演示日志模块
   /* public class Log
    {
        public const int DEBUG = 0;      //调试信息
        public const int INFO = 1;       //普通信息
        public const int WARN = 2;       //警告信息
        public const int ERROR = 3;      //错误信息

        public static int Level = INFO;     //默认当前日志级别

        static string[] levelName = { "DEBUG", "INFO", "WARN", "ERROR" };

        public delegate void PrintCallback(string text);
        public static event PrintCallback Print;

        //加委托
        static Log()
        {
            Log.Print += (text) =>
            {
                Console.WriteLine(text);
            };
        }


        private static void WriteLine(int lev, string text, params object?[]? args)
        {
            if (Level <= lev)//用于隐藏不需要的信息
            {
                text = String.Format(text, args);
                text = String.Format("[{0}]\t-{1}", levelName[lev], text);
                Print?.Invoke(text);
            }
        }
        
        public static void  Debug(string text,params object?[]? args) {
            WriteLine(1, text, args);
        }

        public static void Info(string text, params object?[]? args)
        {
            WriteLine(2, text, args);
        }

        public static void Warn(string text, params object?[]? args)
        {
            WriteLine(3, text, args);
        }

        public static void Error(string text, params object?[]? args)
        {
            WriteLine(4, text, args);
        }
    }*/
    #endregion
}
