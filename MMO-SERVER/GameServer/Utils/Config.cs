using System.IO;
using YamlDotNet.Serialization;

namespace GameServer.Utils
{
    public class DatabaseConfig
    {
        [YamlMember(Alias = "host")]
        public string Host { get; set; }

        [YamlMember(Alias = "port")]
        public int Port { get; set; }

        [YamlMember(Alias = "username")]
        public string Username { get; set; }

        [YamlMember(Alias = "password")]
        public string Password { get; set; }

        [YamlMember(Alias = "dbName")]
        public string DbName { get; set; }
    }

    public class ServerConfig
    {
        [YamlMember(Alias = "gameWorldId")]
        public int gameWorldId;

        [YamlMember(Alias = "ip")]
        public string ip { get; set; }

        [YamlMember(Alias = "serverPort")]
        public int serverPort { get; set; }

        [YamlMember(Alias = "workerCount")]
        public int workerCount { get; set; }        
        
        [YamlMember(Alias = "aoiViewArea")]
        public float aoiViewArea { get; set; }        
        
        [YamlMember(Alias = "updateHz")]
        public int updateHz { get; set; }

        [YamlMember(Alias = "heartBeatTimeOut")]
        public float heartBeatTimeOut { get; set; }

        [YamlMember(Alias = "heartBeatCheckInterval")]
        public float heartBeatCheckInterval { get; set; }

        [YamlMember(Alias = "heartBeatSendInterval")]
        public float heartBeatSendInterval { get; set; }
    }

    public class CCConfig
    {
        [YamlMember(Alias = "ip")]
        public string ip { get; set; }

        [YamlMember(Alias = "port")]
        public int port { get; set; }
    }

    public class AppConfig
    {
        [YamlMember(Alias = "database")]
        public DatabaseConfig Database { get; set; }

        [YamlMember(Alias = "server")]
        public ServerConfig Server { get; set; }

        [YamlMember(Alias = "cc")]
        public CCConfig CCServer { get; set; }
    }

    public static class Config
    {
        private static AppConfig _config;

        public static void Init(string filePath = "config.yaml")
        {
            // 读取配置文件,当前项目根目录
            var yaml = File.ReadAllText(filePath);
            //Log.Information("LoadYamlText:\r\n {Yaml}", yaml);

            // 反序列化配置文件
            var deserializer = new DeserializerBuilder().Build();
            _config = deserializer.Deserialize<AppConfig>(yaml);
        }

        public static DatabaseConfig Database => _config?.Database;
        public static ServerConfig Server => _config?.Server;
        public static CCConfig CCConfig => _config?.CCServer;
    }
}
