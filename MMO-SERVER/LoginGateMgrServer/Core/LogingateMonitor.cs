using Common.Summer.Core;
using Common.Summer.Tools;
using HS.Protobuf.Common;

namespace LoginGateMgrServer.Core
{
    public class LogingateMonitor:Singleton<LogingateMonitor>
    {
        private Dictionary<int, ServerInfoNode> loginInstances = new();
        private Dictionary<int, Connection> logingateInstances = new();
        private Dictionary<int, List<int>> logingateToLoginServerMap = new();   // <loginServerId,{loginGateServerId}>
        private int healthCheckInterval;                                        // 控制健康检查的时间间隔（以秒为单位）。
        private Dictionary<string, int> alertThresholds = new();                // 性能指标阈值
        private Dictionary<int, List<Dictionary<string, int>>> metrics = new(); // 保存每个实例的历史性能指标

        public bool Init()
        {
            healthCheckInterval = 60;
            alertThresholds.Add("cpu_usage", 85);
            alertThresholds.Add("memory_usage", 80);
            Scheduler.Instance.AddTask(RunMonitoring,healthCheckInterval * 1000, 0);

            // 获取loginServer的信息
            LoginGateMgrHandler.Instance.SendGetAllServerInfoRequest();

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public void UpdateLoginServerInfo(List<ServerInfoNode> serverInfoNodes)
        {
            foreach(var node in serverInfoNodes)
            {
                loginInstances[node.ServerId] = node;
            }
        }
        public void RegisterLoginGateInstance(int serverId, Connection conn)
        {
            logingateInstances.Add(serverId, conn);
            AssignLogingateToLoginServer(serverId);

        }
        public void RemoveInstance(int serverId)
        {
            logingateInstances.Remove(serverId);
        }
        public void AssignLogingateToLoginServer(int loginGateserverId)
        {
            if (!logingateInstances.ContainsKey(loginGateserverId))
            {
                return;
            }
            // 判断是否需要分配任务给该gate
            //logingateToLoginServerMap[loginGateserverId] = loginServer;
        }


        // 性能监测相关
        private void PerformHealthCheck()
        {

        }
        private void AnalyzeLogs()
        {

        }
        private void TriggerAlerts()
        {

        }
        public void RunMonitoring()
        {
            PerformHealthCheck();
            AnalyzeLogs();
            TriggerAlerts();
        }
    }
}
