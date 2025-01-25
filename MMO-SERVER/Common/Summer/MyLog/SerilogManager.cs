using Common.Summer.Tools;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using System.Collections.Generic;
namespace Common.Summer.MyLog
{
    public class SerilogManager : Singleton<SerilogManager>
    {
        public bool Init()
        {
            //初始化日志环境
            var customTheme = new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
            {
                [ConsoleThemeStyle.Text]                = "\x1b[37m",           // White
                [ConsoleThemeStyle.SecondaryText]       = "\x1b[37m",           // Gray
                [ConsoleThemeStyle.TertiaryText]        = "\x1b[90m",           // Dark gray
                [ConsoleThemeStyle.Invalid]             = "\x1b[33m",           // Yellow
                [ConsoleThemeStyle.Null]                = "\x1b[34m",           // Blue
                [ConsoleThemeStyle.Name]                = "\x1b[32m",           // Green
                [ConsoleThemeStyle.String]              = "\x1b[36m",           // Cyan
                [ConsoleThemeStyle.Number]              = "\x1b[32m",           // Magenta \x1b[35m
                [ConsoleThemeStyle.Boolean]             = "\x1b[34m",           // Blue
                [ConsoleThemeStyle.Scalar]              = "\x1b[32m",           // Green
                [ConsoleThemeStyle.LevelVerbose]        = "\x1b[90m",           // Dark gray
                [ConsoleThemeStyle.LevelDebug]          = "\x1b[37m",           // White
                [ConsoleThemeStyle.LevelInformation]    = "\x1b[32m",           // Green
                [ConsoleThemeStyle.LevelWarning]        = "\x1b[33m",           // Yellow
                [ConsoleThemeStyle.LevelError]          = "\x1b[31m",           // Red
                [ConsoleThemeStyle.LevelFatal]          = "\x1b[41m\x1b[37m"    // Red background, white text
            });
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    theme: customTheme,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .WriteTo.File(
                    "logs\\server-log.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            return true;
        }
    }
}
