using Common.Summer.Core;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.LoginGate;
using HS.Protobuf.LoginGateMgr;
using LoginGateMgrServer.Net;
using Serilog;

namespace LoginGateMgrServer.Core
{
    class LoginGateEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfo { get; set; }
        public int curLoginServerId { get; set; }
        public LoginGateStatus Status { get; set; }
    }
    class LoginEntry
    {
        public ServerInfoNode ServerInfo { get; set; }
        public int priority
        {
            // 数值越大优先级越高
            get
            {
                if(curGate.Count == 0)
                {
                    return Int32.MaxValue;
                }
                return defaultLoginGateCount - curGate.Count;
            }
        }
        public int defaultLoginGateCount { get; set; }
        public List<int> curGate = new();
    }

    public class LogingateMonitor:Singleton<LogingateMonitor>
    {
        private Dictionary<int, LoginEntry> m_loginInstances = new();
        private Dictionary<int, LoginGateEntry> m_logingateInstances = new();
        private Queue<int> idleQueue = new Queue<int>();

        private int m_healthCheckInterval;                                        // 控制健康检查的时间间隔（以秒为单位）。
        private Dictionary<string, int> m_alertThresholds = new();                // 性能指标阈值
        private Dictionary<int, List<Dictionary<string, int>>> m_metrics = new(); // 保存每个实例的历史性能指标

        public bool Init()
        {
            m_healthCheckInterval = 60;
            m_alertThresholds.Add("cpu_usage", 85);
            m_alertThresholds.Add("memory_usage", 80);
            Scheduler.Instance.AddTask(RunMonitoring,m_healthCheckInterval * 1000, 0);

            // 获取loginServer的信息
            LoginGateMgrHandler.Instance.SendGetAllLoginServerInfoRequest();

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public bool InitLoginServerInfo(List<ServerInfoNode> serverInfoNodes)
        {
            foreach(var node in serverInfoNodes)
            {
                int serverId = node.ServerId;
                LoginEntry entry = new LoginEntry
                {
                    ServerInfo = node,
                    defaultLoginGateCount = 1
                };
                if(!m_loginInstances.ContainsKey(serverId))
                {
                    m_loginInstances.Add(serverId, entry);
                    ProcessIdleQueue(serverId);
                }
            }
            return true;
        }
        public bool AddLoginServerInfo(ServerInfoNode serverInfoNode)
        {
            // todo 有无问题呢？
            // 可能启动初期会有问题？
            var entry = new LoginEntry
            {
                ServerInfo = serverInfoNode,
                defaultLoginGateCount = 1
            };
            m_loginInstances[serverInfoNode.ServerId] = entry;
            ProcessIdleQueue(serverInfoNode.ServerId);
            return true;
        }
        public bool RemoveLoginServerInfo(int serverId)
        {
            if (!m_loginInstances.ContainsKey(serverId))
            {
                return false;
            }
            var lEntry = m_loginInstances[serverId];
            foreach(var lgId in lEntry.curGate)
            {
                var lgEntry = m_logingateInstances[lgId];
                lgEntry.curLoginServerId = -1;
                lgEntry.Status = LoginGateStatus.Inactive;
                ExecuteLGCommandRequest req = new();
                req.TimeStamp = Scheduler.UnixTime;
                req.Command = LoginGateCommand.End;
                lgEntry.Connection.Send(req);
            }
            m_loginInstances.Remove(serverId);
            return true;
        }

        public bool RegisterLoginGateInstance(Connection conn, ServerInfoNode serverInfoNode)
        {
            if (m_logingateInstances.ContainsKey(serverInfoNode.ServerId))
            {
                Log.Error("RegisterLoginGateInstance failed, serverId already exists.");
                return false;
            }
            // todo 有效性判断？
            var entry = new LoginGateEntry
            {
                Connection = conn,
                ServerInfo = serverInfoNode,
                curLoginServerId = -1,
                Status = LoginGateStatus.Inactive,
            };
            m_logingateInstances.Add(serverInfoNode.ServerId, entry);
            AssignTaskToLogingate(serverInfoNode.ServerId);
            return true;
        }
        public bool LoginGateDisconnection(int serverId)
        {
            int relativeLoginServerId = m_logingateInstances[serverId].curLoginServerId;
            if (relativeLoginServerId != -1)
            {
                m_loginInstances[relativeLoginServerId].curGate.Remove(serverId);
            }
            m_logingateInstances.Remove(serverId);
            return true;
        }
        public void AssignTaskToLogingate(int loginGateServerId)
        {
            int loginServerId = -1;

            // 校验
            if (!m_logingateInstances.ContainsKey(loginGateServerId) 
                || m_logingateInstances[loginGateServerId].Status != LoginGateStatus.Inactive)
            {
                goto End;
            }

            // 找一个loginServer
            // 优先处理为0的，在处理需求不满的
            int maxPriority = Int32.MinValue;
            foreach (var item in m_loginInstances)
            {
                if(item.Value.priority > maxPriority)
                {
                    maxPriority = item.Value.priority;
                    loginServerId = item.Key;
                    if(maxPriority == Int32.MaxValue)
                    {
                        break;
                    }
                }
            }

            // 发命令包
            ExecuteLGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            var lgEntry = m_logingateInstances[loginGateServerId];
            if (loginServerId == -1)
            {
                lgEntry.Status = LoginGateStatus.Inactive;
                req.Command = LoginGateCommand.End;
                req.LoginGateServerId = ServersMgr.Instance.ServerId;
                idleQueue.Enqueue(loginGateServerId);
            }
            else
            {
                // 分配任务
                m_loginInstances[loginServerId].curGate.Add(loginGateServerId);
                lgEntry.curLoginServerId = loginServerId;
                lgEntry.Status = LoginGateStatus.Active;
                req.Command = LoginGateCommand.Start;
                req.LoginGateServerId = ServersMgr.Instance.ServerId;
                req.LoginServerInfoNode = m_loginInstances[loginServerId].ServerInfo;
            }
            lgEntry.Connection.Send(req);

        End:
            return;
        }
        private void ProcessIdleQueue(int loginServerId)
        {
            if(idleQueue.Count == 0)
            {
                return;
            }
            LoginEntry entry = m_loginInstances[loginServerId];
            for (int i = 0; i < entry.defaultLoginGateCount - entry.curGate.Count; i++)
            {
                int gateId = idleQueue.Dequeue();
                ActiveGate(gateId, loginServerId);
            }
        }
        private void ActiveGate(int loginGateServerId, int loginServerId) {
            LoginEntry lEntry = m_loginInstances[loginServerId];
            LoginGateEntry gEntry = m_logingateInstances[loginGateServerId];
            lEntry.curGate.Add(loginGateServerId);
            gEntry.curLoginServerId = loginServerId;
            gEntry.Status = LoginGateStatus.Active;

            // 发包
            ExecuteLGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            req.Command = LoginGateCommand.Start;
            req.LoginGateServerId = ServersMgr.Instance.ServerId;
            req.LoginServerInfoNode = lEntry.ServerInfo;
            gEntry.Connection.Send(req);

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
