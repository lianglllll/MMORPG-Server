using Serilog;
using Common.Summer.Core;
using Serilog.Sinks.SystemConsole.Themes;
using GameGateMgrServer.Utils;
using GameGateMgrServer.Net;
using Common.Summer.MyLog;

namespace GameGateMgrServer
{
    class Program
    {
        private static bool Init()
        {
            SerilogManager.Instance.Init();
            Config.Init();                      
            Scheduler.Instance.Start(Config.Server.updateHz);

            Log.Information("\x1b[32m" + @"
                      _____                    _____                    _____          
                     /\    \                  /\    \                  /\    \         
                    /::\    \                /::\    \                /::\____\        
                   /::::\    \              /::::\    \              /::::|   |        
                  /::::::\    \            /::::::\    \            /:::::|   |        
                 /:::/\:::\    \          /:::/\:::\    \          /::::::|   |        
                /:::/  \:::\    \        /:::/  \:::\    \        /:::/|::|   |        
               /:::/    \:::\    \      /:::/    \:::\    \      /:::/ |::|   |        
              /:::/    / \:::\    \    /:::/    / \:::\    \    /:::/  |::|___|______  
             /:::/    /   \:::\ ___\  /:::/    /   \:::\ ___\  /:::/   |::::::::\    \ 
            /:::/____/  ___\:::|    |/:::/____/  ___\:::|    |/:::/    |:::::::::\____\
            \:::\    \ /\  /:::|____|\:::\    \ /\  /:::|____|\::/    / ~~~~~/:::/    /
             \:::\    /::\ \::/    /  \:::\    /::\ \::/    /  \/____/      /:::/    / 
              \:::\   \:::\ \/____/    \:::\   \:::\ \/____/               /:::/    /  
               \:::\   \:::\____\       \:::\   \:::\____\                /:::/    /   
                \:::\  /:::/    /        \:::\  /:::/    /               /:::/    /    
                 \:::\/:::/    /          \:::\/:::/    /               /:::/    /     
                  \::::::/    /            \::::::/    /               /:::/    /      
                   \::::/    /              \::::/    /               /:::/    /       
                    \::/____/                \::/____/                \::/    /        
                                                                       \/____/         
            ");
            Log.Information("[GameGateMgrServer]初始化,配置如下：");
            Log.Information("Ip：{0}", Config.Server.ip);
            Log.Information("ServerPort：{0}", Config.Server.port);
            Log.Information("WorkerCount：{0}", Config.Server.workerCount);
            Log.Information("UpdateHz：{0}", Config.Server.updateHz);
            Log.Information("\x1b[32m" + "=============================================" + "\x1b[0m");

            //开启网络服务
            ServersMgr.Instance.Init();

            return true;
        }
        private static bool UnInit()
        {
            return true;
        }
        private static bool Shell()
        {
            while (true)
            {
                Console.Write("press command to execute:\n"); // Display a prompt
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                // Parse command
                string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = args[0].ToLower();

                switch (command)
                {
                    case "exit":
                        Console.WriteLine("Exiting the shell.");
                        UnInit();
                        Environment.Exit(0);
                        return true;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        break;
                }
            }
        }
        public static void Main(string[] args)
        {
            Init();
            //Shell();
        }
    }
}
