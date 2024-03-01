using System;
using System.Net;
using System.Net.Sockets;
using Summer;
using GameServer.Network;
using Summer.Network;
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

namespace GameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //初始化日志环境
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            //.WriteTo.Console()
            .WriteTo.File("logs\\server-log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
            Log.Debug("[日志服务启动完成]");

            //proto类型加载
            Log.Debug("[装载消息类型如下]");
            ProtoHelper.Init();

            //加载配置文件
            Log.Debug("[加载server配置文件]");
            Config.Init();

            //装载配置文件
            DataManager.Instance.init();
            Log.Debug("[Json配置文件加载完成]");

            //数据库服务
            DbManager.Init();
            Log.Debug("[数据库服务启动完成]");

            //开启网络服务
            //NetService netService = new NetService();
            NetService.Instance.Start();
            Log.Debug("[网络服务启动完成]");

            //开启玩家服务
            UserService.Instance.Start();
            Log.Debug("[玩家服务启动完成]");

            //开启地图服务
            SpaceService.Instance.Start();
            Log.Debug("[地图服务启动完成]");

            //开启战斗服务
            CombatService.Instance.Start();
            Log.Debug("[战斗服务启动完成]");

            //开启频道聊天服务
            ChatService.Instance.start();
            Log.Debug("[频道服务启动完成]");

            //开启物品服务
            ItemService.Instance.start();
            Log.Debug("[物品服务启动完成]");

            //中心计时器任务加载(使用了Timer)
            Scheduler.Instance.Start();
            //添加中心计时器任务：
            Scheduler.Instance.AddTask(() => {
                EntityManager.Instance.Update();
                SpaceManager.Instance.Update();
            }, 0.02f);
            Log.Debug("[中心计时器任务加载完成]");

            //服务器启动完毕，开始工作
            Log.Debug("[mmorpg服务器启动成功!!!]");


            //test 物品实例
            Space space1 = SpaceManager.Instance.GetSpaceById(0);
            var define = DataManager.Instance.ItemDefinedDict[1001];
            var item = new Consumable(define, 1, 0);
            space1.itemManager.Create(item, Vector3Int.zero, Vector3Int.zero);

            var define2 = DataManager.Instance.ItemDefinedDict[1002];
            var item2 = new Consumable(define2, 1, 0);
            space1.itemManager.Create(item2, new Vector3Int(1000,0,1000), Vector3Int.zero);

            Console.ReadKey();//防止进程结束
        }

    }
}
 