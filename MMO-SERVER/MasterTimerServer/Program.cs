using Serilog;
using Common.Summer.Core;
using Common.Summer.MyLog;
using MasterTimerServer.Utils;
using MasterTimerServer.Core;

namespace MasterTimerServer
{
    class Program
    {
        private static bool Init()
        {
            SerilogManager.Instance.Init();
            Config.Init();
            Scheduler.Instance.Start(Config.Server.updateHz);
            ServersMgr.Instance.Init();

            Log.Information("\x1b[32m" + @"
                      _____                _____          
                     /\    \              /\    \         
                    /::\____\            /::\    \        
                   /::::|   |            \:::\    \       
                  /:::::|   |             \:::\    \      
                 /::::::|   |              \:::\    \     
                /:::/|::|   |               \:::\    \    
               /:::/ |::|   |               /::::\    \   
              /:::/  |::|___|______        /::::::\    \  
             /:::/   |::::::::\    \      /:::/\:::\    \ 
            /:::/    |:::::::::\____\    /:::/  \:::\____\
            \::/    / ~~~~~/:::/    /   /:::/    \::/    /
             \/____/      /:::/    /   /:::/    / \/____/ 
                         /:::/    /   /:::/    /          
                        /:::/    /   /:::/    /           
                       /:::/    /    \::/    /            
                      /:::/    /      \/____/             
                     /:::/    /                           
                    /:::/    /                            
                    \::/    /                             
                     \/____/                              
            ");
            Log.Information("[MasterTimerServer]初始化成功,配置如下：");
            Log.Information("Ip：{0}", Config.Server.ip);
            Log.Information("Port：{0}", Config.Server.serverPort);
            Log.Information("WorkerCount：{0}", Config.Server.workerCount);
            Log.Information("UpdateHz：{0}", Config.Server.updateHz);
            Log.Information("\x1b[32m" + "=============================================" + "\x1b[0m");
            Log.Information("\x1b[32m" + "Initialization complete, server is now operational." + "\x1b[0m");
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
                Console.Write("press command to execute:\n");
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
            Shell();
        }
    }
}