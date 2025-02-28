using Serilog;
using SceneServer.Utils;
using Common.Summer.Core;
using Serilog.Sinks.SystemConsole.Themes;
using SceneServer.Net;
using Common.Summer.MyLog;

namespace SceneServer
{
    class Program
    {
        private static bool Init(string[] args)
        {
            SerilogManager.Instance.Init();

            string configPath = "config.yaml";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    configPath = args[i + 1];
                    break;
                }
            }
            Config.Init(configPath);               

            StaticDataManager.Instance.Init();
            Scheduler.Instance.Start(Config.Server.updateHz);

            Log.Information("\x1b[32m" + @"
                      _____          
                     /\    \         
                    /::\    \        
                   /::::\    \       
                  /::::::\    \      
                 /:::/\:::\    \     
                /:::/__\:::\    \    
                \:::\   \:::\    \   
              ___\:::\   \:::\    \  
             /\   \:::\   \:::\    \ 
            /::\   \:::\   \:::\____\
            \:::\   \:::\   \::/    /
             \:::\   \:::\   \/____/ 
              \:::\   \:::\    \     
               \:::\   \:::\____\    
                \:::\  /:::/    /    
                 \:::\/:::/    /     
                  \::::::/    /      
                   \::::/    /       
                    \::/    /        
                     \/____/         
            ");
            Log.Information("[SceneServer]初始化,配置如下：");
            Log.Information("Ip：{0}", Config.Server.ip);
            Log.Information("Port：{0}", Config.Server.serverPort);
            Log.Information("WorkerCount：{0}", Config.Server.workerCount);
            Log.Information("UpdateHz：{0}", Config.Server.updateHz);
            Log.Information("AoiViewArea：{0}", Config.Server.aoiViewArea);
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
            Init(args);
            //Shell();
        }
    }
}
 