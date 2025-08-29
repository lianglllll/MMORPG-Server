using Serilog;
using Common.Summer.Core;
using Common.Summer.MyLog;
using MasterTimerServer.Utils;
using MasterTimerServer.Core;
using MasterTimerServer.Net;

namespace MasterTimerServer
{
    class Program
    {
        private static bool Init()
        {
            Config.Init();

            SerilogManager.Instance.Init();
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
            Log.Information("[MasterTimerServer]初始化配置：{@Config}", Config.Server);
            Scheduler.Instance.Start(Config.Server.updateHz);
            MasterTimerServersMgr.Instance.Init();

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
            // Shell();
        }
    }
}