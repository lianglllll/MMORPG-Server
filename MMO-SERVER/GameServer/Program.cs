using System;
using System.Net;
using System.Net.Sockets;
using GameServer;
using Proto;
using Serilog;
using GameServer.Service;
using GameServer.Database;
using GameServer.Model;
using GameServer.Manager;
using GameServer.AI;
using GameServer.InventorySystem;
using GameServer.Core;
using GameServer.Utils;
using GameServer.Skills;
using GameServer.AI.BehaviorTree;
using GameServer.Network;

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //初始化日志环境
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs\\server-log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
            Log.Debug("[日志服务启动]");

            //加载配置文件
            Log.Debug("[加载server配置信息]");
            Config.Init();

            //装载配置文件
            Log.Debug("[加载Json配置文件]");
            DataManager.Instance.Init();

            //proto类型加载
            Log.Debug("[proto类型加载]");
            ProtoHelper.Init();

            //加载自定义技能类
            Log.Debug("[加载自定义技能类]");
            SkillSanner.Start();

            //数据库服务
            Log.Debug("[启动数据库服务]");
            DbManager.Init();

            //开启网络服务
            Log.Debug("[启动网络服务]");
            NetService.Instance.Start();

            //开启玩家服务
            Log.Debug("[启动玩家服务]");
            UserService.Instance.Start();

            //开启地图服务
            Log.Debug("[启动地图服务]");
            SpaceService.Instance.Start();

            //开启战斗服务
            Log.Debug("[启动战斗服务]");
            CombatService.Instance.Start();

            //开启频道聊天服务
            Log.Debug("[启动频道服务]");
            ChatService.Instance.Start();

            //开启物品服务
            Log.Debug("[启动物品服务]");
            ItemService.Instance.Start();

            //中心计时器任务加载(使用了Timer)
            Log.Debug("[激活世界心跳Tick]");
            Scheduler.Instance.Start();
            //添加中心计时器任务：
            Scheduler.Instance.AddTask(() => {
                EntityManager.Instance.Update();
                SpaceManager.Instance.Update();
            }, Config.Server.UpdateHz);

            //防止进程结束
            Console.ReadKey();
        }
    }
}
 