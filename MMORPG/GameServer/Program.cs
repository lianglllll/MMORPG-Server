using System;
using Serilog;
using GameServer.Net;
using GameServer.Utils;
using Common.Summer.Core;
using Serilog.Sinks.SystemConsole.Themes;
using System.Collections.Generic;
using Common.Summer.MyLog;
using Newtonsoft.Json;
using GameServer.Core.Task;
using GameServer.Core.Model;

namespace GameServer
{
    class Program
    {
        private static bool Init()
        {

            SerilogManager.Instance.Init();
            Config.Init();                      // 加载服务器配置
            //DataManager.Instance.Init();        // 加载json配置文件
            //SkillSanner.Start();                // 加载自定义技能类
            //DbManager.Init();                   
            //UserService.Instance.Start();       
            //SpaceService.Instance.Start();      
            //CombatService.Instance.Start();
            //ChatService.Instance.Start();
            //ItemService.Instance.Start();

            //中心计时器任务加载(使用了Timer)
            Scheduler.Instance.Start(Config.Server.updateHz);
            ////添加中心计时器任务：
            //Scheduler.Instance.AddTask(() => {
            //    EntityManager.Instance.Update();
            //    SpaceManager.Instance.Update();
            //}, Config.Server.updateHz);

            Log.Information("\x1b[32m" + @"
                      _____          
                     /\    \         
                    /::\    \        
                   /::::\    \       
                  /::::::\    \      
                 /:::/\:::\    \     
                /:::/  \:::\    \    
               /:::/    \:::\    \   
              /:::/    / \:::\    \  
             /:::/    /   \:::\ ___\ 
            /:::/____/  ___\:::|    |
            \:::\    \ /\  /:::|____|
             \:::\    /::\ \::/    / 
              \:::\   \:::\ \/____/  
               \:::\   \:::\____\    
                \:::\  /:::/    /    
                 \:::\/:::/    /     
                  \::::::/    /      
                   \::::/    /       
                    \::/____/        
            ");
            Log.Information("[GameServer]初始化,配置如下：");
            Log.Information("Ip：{0}", Config.Server.ip);
            Log.Information("ServerPort：{0}", Config.Server.serverPort);
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
                    case "LevelUp":
                        foreach(var item in GameCharacterManager.Instance.GetAllGameCharacter().Values)
                        {
                            item.CharacterEventSystem.Trigger("LevelUp");
                            break;
                        }
                        break;
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
 