using GameServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Database
{
    public class DbManager
    {
        //IFreeSql 是 ORM 最顶级对象，所有操作都是使用它的方法或者属性：
        public static IFreeSql fsql;

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            //一些配置参数
            string ip = Config.Database.Host;
            int port = Config.Database.Port;
            string user = Config.Database.Username;
            string password = Config.Database.Password;
            string dbName = Config.Database.DbName;

            //配置信息的字符串
            string connectionString =
                $"Data Source={ip};Port={port};User ID={user};Password={password};" +
                $"Initial Catalog={dbName};Charset=utf8;SslMode=none;Max pool size=10";

            fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(FreeSql.DataType.MySql, connectionString)
                .UseAutoSyncStructure(true) //自动同步实体结构到数据库
                .Build(); //请务必定义成 Singleton 单例模式
        }

    }
}
