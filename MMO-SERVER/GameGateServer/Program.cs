using Serilog;
using Common.Summer.Core;
using Serilog.Sinks.SystemConsole.Themes;
using GameGateServer.Net;
using GameGateServer.Utils;
using Common.Summer.MyLog;

namespace GameGateServer
{
    class Program
    {
        private static bool Init()
        {
            SerilogManager.Instance.Init();
            Config.Init();

            Log.Information("\x1b[32m" + @"
                      _____                    _____          
                     /\    \                  /\    \         
                    /::\    \                /::\    \        
                   /::::\    \              /::::\    \       
                  /::::::\    \            /::::::\    \      
                 /:::/\:::\    \          /:::/\:::\    \     
                /:::/  \:::\    \        /:::/  \:::\    \    
               /:::/    \:::\    \      /:::/    \:::\    \   
              /:::/    / \:::\    \    /:::/    / \:::\    \  
             /:::/    /   \:::\ ___\  /:::/    /   \:::\ ___\ 
            /:::/____/  ___\:::|    |/:::/____/  ___\:::|    |
            \:::\    \ /\  /:::|____|\:::\    \ /\  /:::|____|
             \:::\    /::\ \::/    /  \:::\    /::\ \::/    / 
              \:::\   \:::\ \/____/    \:::\   \:::\ \/____/  
               \:::\   \:::\____\       \:::\   \:::\____\    
                \:::\  /:::/    /        \:::\  /:::/    /    
                 \:::\/:::/    /          \:::\/:::/    /     
                  \::::::/    /            \::::::/    /      
                   \::::/    /              \::::/    /       
                    \::/____/                \::/____/        
            ");
            Log.Information("[GameGateServer]初始化,配置如下：");
            Log.Information("Ip：{0}", Config.Server.ip);
            Log.Information("UserPort：{0}", Config.Server.userPort);
            Log.Information("ServerPort：{0}", Config.Server.serverPort);
            Log.Information("WorkerCount：{0}", Config.Server.workerCount);
            Log.Information("UpdateHz：{0}", Config.Server.updateHz);
            Log.Information("\x1b[32m" + "=============================================" + "\x1b[0m");

            Scheduler.Instance.Start(Config.Server.updateHz);
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
