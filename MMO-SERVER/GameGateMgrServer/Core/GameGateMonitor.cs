using Common.Summer.Core;
using Common.Summer.Tools;
using GameGateMgrServer.Net;
using HS.Protobuf.Common;
using HS.Protobuf.LoginGate;
using HS.Protobuf.LoginGateMgr;
using Serilog;

namespace GameGateMgrServer.Core
{
    class GameGateEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfo { get; set; }
        public int curLoginServerId { get; set; }
        public GateStatus Status { get; set; }
    }
    class GameEntry
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

    public class GameGateMonitor:Singleton<GameGateMonitor>
    {
        private Dictionary<int, GameEntry> m_gameInstances = new();
        private Dictionary<int, GameGateEntry> m_gameGateInstances = new();
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

            // 获取gameServer的信息
            GameGateMgrHandler.Instance.SendGetAllGameServerInfoRequest();

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public bool InitGameServerInfo(List<ServerInfoNode> serverInfoNodes)
        {
            foreach(var node in serverInfoNodes)
            {
                int serverId = node.ServerId;
                GameEntry entry = new GameEntry
                {
                    ServerInfo = node,
                    defaultLoginGateCount = 1
                };
                if(!m_gameInstances.ContainsKey(serverId))
                {
                    m_gameInstances.Add(serverId, entry);
                    ProcessIdleQueue(serverId);
                }
            }
            return true;
        }
        public bool AddGameServerInfo(ServerInfoNode serverInfoNode)
        {
            // todo 有无问题呢？
            // 可能启动初期会有问题？
            var entry = new GameEntry
            {
                ServerInfo = serverInfoNode,
                defaultLoginGateCount = 1
            };
            m_gameInstances[serverInfoNode.ServerId] = entry;
            ProcessIdleQueue(serverInfoNode.ServerId);
            return true;
        }
        public bool RemoveGameServerInfo(int serverId)
        {
            if (!m_gameInstances.ContainsKey(serverId))
            {
                return false;
            }
            var gEntry = m_gameInstances[serverId];
            foreach(var ggId in gEntry.curGate)
            {
                var ggEntry = m_gameGateInstances[ggId];
                ggEntry.curLoginServerId = -1;
                ggEntry.Status = GateStatus.Inactive;
                ExecuteLGCommandRequest req = new();
                req.TimeStamp = Scheduler.UnixTime;
                req.Command = GateCommand.End;
                ggEntry.Connection.Send(req);
            }
            m_gameInstances.Remove(serverId);
            return true;
        }

        public bool RegisterGameGateInstance(Connection conn, ServerInfoNode serverInfoNode)
        {
            if (m_gameGateInstances.ContainsKey(serverInfoNode.ServerId))
            {
                Log.Error("RegisterGameGateInstance failed, serverId already exists.");
                return false;
            }
            // todo 有效性判断？
            var entry = new GameGateEntry
            {
                Connection = conn,
                ServerInfo = serverInfoNode,
                curLoginServerId = -1,
                Status = GateStatus.Inactive,
            };
            m_gameGateInstances.Add(serverInfoNode.ServerId, entry);
            AssignTaskToGameGate(serverInfoNode.ServerId);
            return true;
        }
        public bool GameGateDisconnection(int serverId)
        {
            int relativeLoginServerId = m_gameGateInstances[serverId].curLoginServerId;
            if (relativeLoginServerId != -1)
            {
                m_gameInstances[relativeLoginServerId].curGate.Remove(serverId);
            }
            m_gameGateInstances.Remove(serverId);
            return true;
        }
        public void AssignTaskToGameGate(int gameGateServerId)
        {
            int gameServerId = -1;

            // 校验
            if (!m_gameGateInstances.ContainsKey(gameGateServerId) 
                || m_gameGateInstances[gameGateServerId].Status != GateStatus.Inactive)
            {
                goto End;
            }

            // 找一个loginServer
            // 优先处理为0的，在处理需求不满的
            int maxPriority = Int32.MinValue;
            foreach (var item in m_gameInstances)
            {
                if(item.Value.priority > maxPriority)
                {
                    maxPriority = item.Value.priority;
                    gameServerId = item.Key;
                    if(maxPriority == Int32.MaxValue)
                    {
                        break;
                    }
                }
            }

            // 发命令包
            ExecuteLGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            var ggEntry = m_gameGateInstances[gameGateServerId];
            if (gameServerId == -1)
            {
                ggEntry.Status = GateStatus.Inactive;
                req.Command = GateCommand.End;
                req.LoginGateServerId = ServersMgr.Instance.ServerId;
                idleQueue.Enqueue(gameGateServerId);
            }
            else
            {
                // 分配任务
                m_gameInstances[gameServerId].curGate.Add(gameGateServerId);
                ggEntry.curLoginServerId = gameServerId;
                ggEntry.Status = GateStatus.Active;
                req.Command = GateCommand.Start;
                req.LoginGateServerId = ServersMgr.Instance.ServerId;
                req.LoginServerInfoNode = m_gameInstances[gameServerId].ServerInfo;
            }
            ggEntry.Connection.Send(req);

        End:
            return;
        }
        private void ProcessIdleQueue(int gameServerId)
        {
            if(idleQueue.Count == 0)
            {
                return;
            }
            GameEntry entry = m_gameInstances[gameServerId];
            for (int i = 0; i < entry.defaultLoginGateCount - entry.curGate.Count; i++)
            {
                int gateId = idleQueue.Dequeue();
                ActiveGate(gateId, gameServerId);
            }
        }
        private void ActiveGate(int gameGateServerId, int gameServerId) {
            GameEntry gEntry = m_gameInstances[gameServerId];
            GameGateEntry ggEntry = m_gameGateInstances[gameGateServerId];
            gEntry.curGate.Add(gameGateServerId);
            ggEntry.curLoginServerId = gameServerId;
            ggEntry.Status = GateStatus.Active;

            // 发包
            ExecuteLGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            req.Command = GateCommand.Start;
            req.LoginGateServerId = ServersMgr.Instance.ServerId;
            req.LoginServerInfoNode = gEntry.ServerInfo;
            ggEntry.Connection.Send(req);
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
