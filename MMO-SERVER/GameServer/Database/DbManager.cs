using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Database
{
    class DbManager
    {

        //一些配置参数，todo应该将其写道一个配置文件当中，再解析到当前类
        private static string ip = "127.0.0.1";
        private static int port = 3306;
        private static string user = "root";
        private static string password = "root";
        private static string dbName = "mmorpg";

        //配置信息的字符串
        private static string connectionString = 
            $"Data Source={ip};Port={port};User ID={user};Password={password};" +
            $"Initial Catalog={dbName};Charset=utf8;SslMode=none;Max pool size=10";

        //IFreeSql 是 ORM 最顶级对象，所有操作都是使用它的方法或者属性：
        public static IFreeSql fsql = new FreeSql.FreeSqlBuilder()
            .UseConnectionString(FreeSql.DataType.MySql, connectionString)
            .UseAutoSyncStructure(true) //自动同步实体结构到数据库
            .Build(); //请务必定义成 Singleton 单例模式

    }
}
