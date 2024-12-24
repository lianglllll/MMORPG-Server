using YamlDotNet.Serialization;

namespace ControlCenter.Utils
{
    public class ServerConfig
    {
        [YamlMember(Alias = "ip")]
        public string ip { get; set; }

        [YamlMember(Alias = "port")]
        public int port { get; set; }

        [YamlMember(Alias = "workerCount")]
        public int workerCount { get; set; }        
        
        [YamlMember(Alias = "updateHz")]
        public int updateHz { get; set; }

        [YamlMember(Alias = "heartBeatTimeOut")]
        public float heartBeatTimeOut { get; set; }

        [YamlMember(Alias = "heartBeatCheckInterval")]
        public float heartBeatCheckInterval { get; set; }
    }

    public class AppConfig
    {
        [YamlMember(Alias = "server")]
        public ServerConfig Server { get; set; }
    }

    public static class Config
    {
        private static AppConfig _config;
        public static void Init(string filePath = "config.yaml")
        {
            // 读取配置文件,当前项目根目录
            var yaml = File.ReadAllText(filePath);

            // 反序列化配置文件
            var deserializer = new DeserializerBuilder().Build();
            _config = deserializer.Deserialize<AppConfig>(yaml);
        }
        public static ServerConfig Server => _config?.Server;
    }
}
