using Serilog;
using Common.Summer.Core;
using Serilog.Sinks.SystemConsole.Themes;
using DBProxyServer.Utils;
using DBProxyServer.Net;
using DBProxyServer.Core;
using DBProxyServer.Handle;
using HS.Protobuf.DBProxy.DBWorld;
using Common.Summer.MyLog;

namespace DBProxyServer
{
    class Program
    {
        private static bool Init()
        {
            SerilogManager.Instance.Init();
            Config.Init();                      
            Scheduler.Instance.Start(Config.Server.updateHz);

            Log.Information("\x1b[32m" + @"
                      _____                    _____          
                     /\    \                  /\    \         
                    /::\    \                /::\    \        
                   /::::\    \              /::::\    \       
                  /::::::\    \            /::::::\    \      
                 /:::/\:::\    \          /:::/\:::\    \     
                /:::/  \:::\    \        /:::/__\:::\    \    
               /:::/    \:::\    \      /::::\   \:::\    \   
              /:::/    / \:::\    \    /::::::\   \:::\    \  
             /:::/    /   \:::\ ___\  /:::/\:::\   \:::\ ___\ 
            /:::/____/     \:::|    |/:::/__\:::\   \:::|    |
            \:::\    \     /:::|____|\:::\   \:::\  /:::|____|
             \:::\    \   /:::/    /  \:::\   \:::\/:::/    / 
              \:::\    \ /:::/    /    \:::\   \::::::/    /  
               \:::\    /:::/    /      \:::\   \::::/    /   
                \:::\  /:::/    /        \:::\  /:::/    /    
                 \:::\/:::/    /          \:::\/:::/    /     
                  \::::::/    /            \::::::/    /      
                   \::::/    /              \::::/    /       
                    \::/____/                \::/____/        
                     ~~                       ~~              
            ");
            Log.Information("[DBProxyServer]初始化,配置如下：");
            Log.Information("Ip：{0}", Config.Server.ip);
            Log.Information("ServerPort：{0}", Config.Server.serverPort);
            Log.Information("WorkerCount：{0}", Config.Server.workerCount);
            Log.Information("UpdateHz：{0}", Config.Server.updateHz);
            Log.Information("\x1b[32m" + "=============================================" + "\x1b[0m");

            // DB
            MongoDBConnection.Instance.Init(Config.MongodbServerConfig.connectionString, 
                Config.MongodbServerConfig.databaseName);
            //开启网络服务
            ServersMgr.Instance.Init();
            UserHandler.Instance.Init();
            CharacterHandler.Instance.Init();
            WorldHandler.Instance.Init();
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
        private static bool Test()
        {
            // HandleUser.Instance._HandleGetDBUserRequest(null, new GetDBUserRequest { UserName = "南风君" });
            // HandleUser.Instance._HandleAddDBUserRequset(null, new AddDBUserRequset { UserName = "令狐冲",Password = "123" });

            //var testCharacterNode = new DBCharacterNode
            //{
            //    UId = "6773c09262629ff8c338d7bf",
            //    ProfessionId = 1,
            //    ChrName = "Hero",
            //    ChrStatistics = new DBCharacterStatisticsNode
            //    {
            //        KillCount = 100
            //    },
            //    ChrStatus = new DBCharacterStatusNode
            //    {
            //        Hp = 1000,
            //        Mp = 500,
            //        Level = 10,
            //        Exp = 2000,
            //        CurSpaceId = 1,
            //        X = 100,
            //        Y = 200,
            //        Z = 300
            //    },
            //    ChrAssets = new DBCharacterAssetsNode
            //    {
            //        // 示例背包数据：可以是物品ID和数量的序列
            //        BackpackData = ByteString.CopyFrom(new byte[] { 0x01, 0xFF, 0x23, 0x7C, 0x10 }),

            //        // 示例装备数据：可以是装备ID和属性的序列
            //        EquipsData = ByteString.CopyFrom(new byte[] { 0x02, 0xAB, 0x4D, 0x5E, 0x99 })
            //    }
            //};
            //HandleCharacter.Instance._HandleAddDBCharacterRequset(null, new AddDBCharacterRequset { ChrNode = testCharacterNode });

            // HandleCharacter.Instance._HandleGetDBCharacterRequest(null, new GetDBCharacterRequest { CId = "6773a543be0a1a051df27169" });

            // UserOperations.Instance.DeleteUserByUidAsync("67736c9f466dcc4edfb62a56");

            // CharacterOperations.Instance.DeleteCharacterByCidAsync("6773c0b050083e9841698d37");
            /*
            //WorldOperations.Instance.AddWorldAsync(new HS.Protobuf.DBProxy.DBWorld.DBWorldNode
            //{
            //    WorldId = 1,
            //    WorldName = "小南梁界",
            //    WorldDesc = "环界？？",
            //    IsActive = true
            //});

            //var testNode = new DBWorldNode
            //{
            //    WorldId = 1,
            //    WorldName = "小南梁界",
            //    WorldDesc = "领略修仙和科技的碰撞。",
            //    Status = "active",
            //    CreatedAt = Scheduler.UnixTime,
            //    MaxPlayers = 1000,
            //    CreatedBy = "天道"
            //};
            //var result =  WorldOperations.Instance.AddWorldAsync(testNode);

            //var testNode2 = new DBWorldNode
            //{
            //    WorldId = 1,
            //    WorldName = "小南梁界01",
            //    WorldDesc = "什么飞升仙界，不过是大一点的牲畜圈养地。",
            //    Status = "inActive",
            //    CreatedAt = Scheduler.UnixTime,
            //    MaxPlayers = 1000,
            //    CreatedBy = "天道"
            //};
            //var result2 = WorldOperations.Instance.AddWorldAsync(testNode2);



            //UserHandler.Instance._HandleAddDBUserRequset(null, new AddDBUserRequset
            //{
            //    DbUserNode = new DBUserNode
            //    {
            //        UserName = "xiaoliangba",
            //        Password = "xiaoxiao",
            //        AccessLevel = "admin"
            //    }
            //});

            */


            return true;
        }
        public static void Main(string[] args)
        {
            Init();
            Test();
            //Shell();
        }
    }
}
