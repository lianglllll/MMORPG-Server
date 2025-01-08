using Common.Summer.Core;
using Common.Summer.Tools;
using GameGateMgrServer.Net;
using Google.Protobuf.Collections;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.GameGateMgr;
using HS.Protobuf.LoginGate;
using HS.Protobuf.LoginGateMgr;
using Serilog;

namespace GameGateMgrServer.Core
{
    class GameGateEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfo { get; set; }
        public int curGameServerId { get; set; }
        public ServerStatus Status { get; set; }
    }
    class SceneEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfo { get; set; }
        public int curGameServerId { get; set; }
        public ServerStatus Status { get; set; }
    }
    class GameEntry
    {
        public ServerInfoNode ServerInfo { get; set; }
        public int AssignGatePriority
        {
            // 数值越大优先级越高
            get
            {
                if(curGate.Count == 0)
                {
                    return Int32.MaxValue;
                }
                return NeedLoginGateCount - curGate.Count;
            }
        }
        public int NeedLoginGateCount { get; set; }
        public List<int> curGate = new();

        public int AssignScenePriority { 
            get
            {
                if (curGate.Count == 0)
                {
                    return Int32.MaxValue;
                }
                return NeedLoginGateCount - curGate.Count;
            }
        }
        public int NeedSceneCount { get; set; }
        public List<int> curScene = new();
    }

    public class GameGateMonitor:Singleton<GameGateMonitor>
    {
        private Dictionary<int, GameEntry> m_gameInstances = new();
        private Dictionary<int, GameGateEntry> m_gameGateInstances = new();
        private Dictionary<int, SceneEntry> m_sceneInstances = new();
        private Queue<int> gameGateIdleQueue = new Queue<int>();
        private Queue<int> sceneIdleQueue = new Queue<int>();

        private int m_healthCheckInterval;                                        // 控制健康检查的时间间隔（以秒为单位）。
        private Dictionary<string, int> m_alertThresholds = new();                // 性能指标阈值
        private Dictionary<int, List<Dictionary<string, int>>> m_metrics = new(); // 保存每个实例的历史性能指标

        public bool Init()
        {
            m_healthCheckInterval = 60;
            m_alertThresholds.Add("cpu_usage", 85);
            m_alertThresholds.Add("memory_usage", 80);
            Scheduler.Instance.AddTask(RunMonitoring,m_healthCheckInterval * 1000, 0);

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public bool AddGameServerInfos(RepeatedField<ClusterEventNode> clusterEventNodes)
        {
            foreach (var node in clusterEventNodes)
            {
                if (node.EventType == ClusterEventType.GameEnter)
                {
                    AddGameServerInfo(node.ServerInfoNode);
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
                NeedLoginGateCount = 1
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
                ggEntry.curGameServerId = -1;
                ggEntry.Status = ServerStatus.Inactive;
                ExecuteLGCommandRequest req = new();
                req.TimeStamp = Scheduler.UnixTime;
                req.Command = GateCommand.End;
                ggEntry.Connection.Send(req);
            }
            m_gameInstances.Remove(serverId);
            return true;
        }

        // gate && scene
        public bool RegisterToGGMInstance(Connection conn, ServerInfoNode serverInfoNode)
        {
            if (m_gameGateInstances.ContainsKey(serverInfoNode.ServerId))
            {
                Log.Error("RegisterGameGateInstance failed, serverId already exists.");
                return false;
            }

            if (serverInfoNode.ServerType == SERVER_TYPE.Game)
            {
                var entry = new GameGateEntry
                {
                    Connection = conn,
                    ServerInfo = serverInfoNode,
                    curGameServerId = -1,
                    Status = ServerStatus.Inactive,
                };
                m_gameGateInstances.Add(serverInfoNode.ServerId, entry);
            }
            else if (serverInfoNode.ServerType == SERVER_TYPE.Scene)
            {
                var entry = new SceneEntry
                {
                    Connection = conn,
                    ServerInfo = serverInfoNode,
                    curGameServerId = -1,
                    Status = ServerStatus.Inactive,
                };
                m_sceneInstances.Add(serverInfoNode.ServerId, entry);
            }

            _AssignTaskToGameGate(serverInfoNode.ServerId);
            return true;
        }
        public bool EntryDisconnection(int serverId)
        {
            if (m_gameGateInstances.ContainsKey(serverId))
            {
                return _GameGateDisconnection(serverId);
            }
            else if (m_sceneInstances.ContainsKey(serverId))
            {
                return _SceneDisconnection(serverId);
            }
            return false;
        }
        private bool _GameGateDisconnection(int serverId)
        {
            int relativeLoginServerId = m_gameGateInstances[serverId].curGameServerId;
            if (relativeLoginServerId != -1)
            {
                m_gameInstances[relativeLoginServerId].curGate.Remove(serverId);
            }
            m_gameGateInstances.Remove(serverId);
            return true;
        }
        private bool _SceneDisconnection(int serverId)
        {
            int relativeLoginServerId = m_gameGateInstances[serverId].curGameServerId;
            if (relativeLoginServerId != -1)
            {
                m_gameInstances[relativeLoginServerId].curScene.Remove(serverId);
            }
            m_sceneInstances.Remove(serverId);
            return true;
        }

        public void _AssignTaskToGameGate(int gameGateServerId)
        {
            int gameServerId = -1;

            // 校验
            if (!m_gameGateInstances.ContainsKey(gameGateServerId) 
                || m_gameGateInstances[gameGateServerId].Status != ServerStatus.Inactive)
            {
                goto End;
            }

            // 找一个loginServer
            // 优先处理为0的，在处理需求不满的
            int maxPriority = Int32.MinValue;
            foreach (var item in m_gameInstances)
            {
                if(item.Value.AssignGatePriority > maxPriority)
                {
                    maxPriority = item.Value.AssignGatePriority;
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
                ggEntry.Status = ServerStatus.Inactive;
                req.Command = GateCommand.End;
                req.LoginGateServerId = ServersMgr.Instance.ServerId;
                gameGateIdleQueue.Enqueue(gameGateServerId);
            }
            else
            {
                // 分配任务
                m_gameInstances[gameServerId].curGate.Add(gameGateServerId);
                ggEntry.curGameServerId = gameServerId;
                ggEntry.Status = ServerStatus.Active;
                req.Command = GateCommand.Start;
                req.LoginGateServerId = ServersMgr.Instance.ServerId;
                req.LoginServerInfoNode = m_gameInstances[gameServerId].ServerInfo;
            }
            ggEntry.Connection.Send(req);

        End:
            return;
        }
        public void _AssignTaskToScene(int sceneServerId)
        {
            // 校验
            if (!m_sceneInstances.ContainsKey(sceneServerId)
                || m_sceneInstances[sceneServerId].Status != ServerStatus.Inactive)
            {
                goto End;
            }

            // 找一个loginServer
            // 优先处理为0的，在处理需求不满的
            int gameServerId = -1;
            int maxPriority = Int32.MinValue;
            foreach (var item in m_gameInstances)
            {
                if (item.Value.AssignScenePriority > maxPriority)
                {
                    maxPriority = item.Value.AssignScenePriority;
                    gameServerId = item.Key;
                    if (maxPriority == Int32.MaxValue)
                    {
                        break;
                    }
                }
            }

            // 发命令包
            ExecuteSCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            var sEntry = m_gameGateInstances[sceneServerId];
            if (gameServerId == -1)
            {
                sEntry.Status = ServerStatus.Inactive;
                req.SceneServerId = ServersMgr.Instance.ServerId;
                sceneIdleQueue.Enqueue(sceneServerId);
            }
            else
            {
                // 分配任务
                m_gameInstances[gameServerId].curScene.Add(sceneServerId);
                sEntry.curGameServerId = gameServerId;
                sEntry.Status = ServerStatus.Active;
                req.SceneServerId = ServersMgr.Instance.ServerId;
                req.GameServerInfoNode = m_sceneInstances[gameServerId].ServerInfo;
            }
            sEntry.Connection.Send(req);

        End:
            return;
        }
        private void ProcessIdleQueue(int gameServerId)
        {
            if(gameGateIdleQueue.Count != 0)
            {
                GameEntry entry = m_gameInstances[gameServerId];
                for (int i = 0; i < entry.NeedLoginGateCount - entry.curGate.Count; i++)
                {
                    int gateId = gameGateIdleQueue.Dequeue();
                    ActiveGate(gateId, gameServerId);
                    if(gameGateIdleQueue.Count == 0)
                    {
                        break;
                    }
                }
            }
            if (sceneIdleQueue.Count != 0)
            {
                GameEntry entry = m_gameInstances[gameServerId];
                for (int i = 0; i < entry.NeedSceneCount - entry.curScene.Count; i++)
                {
                    int sceneId = gameGateIdleQueue.Dequeue();
                    ActiveScene(sceneId, gameServerId);
                    if(sceneIdleQueue.Count == 0)
                    {
                        break;
                    }
                }
            }

        }
        private void ActiveGate(int gameGateServerId, int gameServerId) {
            GameEntry gEntry = m_gameInstances[gameServerId];
            GameGateEntry ggEntry = m_gameGateInstances[gameGateServerId];
            gEntry.curGate.Add(gameGateServerId);
            ggEntry.curGameServerId = gameServerId;
            ggEntry.Status = ServerStatus.Active;

            // 发包
            ExecuteLGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            req.Command = GateCommand.Start;
            req.LoginGateServerId = ServersMgr.Instance.ServerId;
            req.LoginServerInfoNode = gEntry.ServerInfo;
            ggEntry.Connection.Send(req);
        }
        private void ActiveScene(int sceneServerId, int gameServerId)
        {
            GameEntry gEntry = m_gameInstances[gameServerId];
            SceneEntry sEntry = m_sceneInstances[sceneServerId];
            gEntry.curScene.Add(sceneServerId);
            sEntry.curGameServerId = gameServerId;
            sEntry.Status = ServerStatus.Active;

            // 发包
            ExecuteSCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            req.SceneServerId = ServersMgr.Instance.ServerId;
            req.GameServerInfoNode = gEntry.ServerInfo;
            sEntry.Connection.Send(req);
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
