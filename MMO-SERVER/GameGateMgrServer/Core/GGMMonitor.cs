using Common.Summer.Core;
using Common.Summer.Tools;
using GameGateMgrServer.Net;
using Google.Protobuf.Collections;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.GameGate;
using HS.Protobuf.GameGateMgr;
using HS.Protobuf.LoginGate;
using HS.Protobuf.LoginGateMgr;
using Serilog;

namespace GameGateMgrServer.Core
{
    class GameEntry
    {
        public ServerInfoNode ServerInfo { get; set; }
        public int AssignGatePriority
        {
            // 数值越大优先级越高
            get
            {
                if (curGate.Count == 0)
                {
                    return Int32.MaxValue;
                }
                return NeedGameGateCount - curGate.Count;
            }
        }
        public int NeedGameGateCount { get; set; }
        public List<int> curGate = new();
        public int assignSessionIndex { get; set; }
        public int AssignScenePriority
        {
            get
            {
                if (curGate.Count == 0)
                {
                    return Int32.MaxValue;
                }
                return NeedGameGateCount - curGate.Count;
            }
        }
        public int NeedSceneCount { get; set; }
        public List<int> curScene = new();
    }
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

    public class GGMMonitor:Singleton<GGMMonitor>
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

        // game
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
                NeedGameGateCount = 1,
                NeedSceneCount = 1,
                assignSessionIndex = 0,
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


        // gameGate && scene
        public bool RegisterToGGMInstance(Connection conn, ServerInfoNode serverInfoNode)
        {
            if (m_gameGateInstances.ContainsKey(serverInfoNode.ServerId))
            {
                Log.Error("Register GameGateInstance failed, serverId already exists.");
                return false;
            }

            if (serverInfoNode.ServerType == SERVER_TYPE.Gamegate)
            {
                Log.Information("Register GameGateInstance , {0}", serverInfoNode);
                var entry = new GameGateEntry
                {
                    Connection = conn,
                    ServerInfo = serverInfoNode,
                    curGameServerId = -1,
                    Status = ServerStatus.Inactive,
                };
                m_gameGateInstances.Add(serverInfoNode.ServerId, entry);
                _AssignTaskToGameGate(serverInfoNode.ServerId);
            }
            else if (serverInfoNode.ServerType == SERVER_TYPE.Scene)
            {
                Log.Information("Register SceneInstance , {0}", serverInfoNode);
                var entry = new SceneEntry
                {
                    Connection = conn,
                    ServerInfo = serverInfoNode,
                    curGameServerId = -1,
                    Status = ServerStatus.Inactive,
                };
                m_sceneInstances.Add(serverInfoNode.ServerId, entry);
                _AssignTaskToScene(serverInfoNode.ServerId);
            }

            return true;
        }
        public void _AssignTaskToGameGate(int gameGateServerId)
        {

            // 校验
            if (!m_gameGateInstances.ContainsKey(gameGateServerId)
                || m_gameGateInstances[gameGateServerId].Status != ServerStatus.Inactive)
            {
                goto End;
            }

            // 找一个loginServer
            // 优先处理为0的，在处理需求不满的
            GameEntry gameEntry = null;
            int maxPriority = Int32.MinValue;
            foreach (var item in m_gameInstances)
            {
                if (item.Value.AssignGatePriority > maxPriority)
                {
                    maxPriority = item.Value.AssignGatePriority;
                    gameEntry = item.Value;
                    if (maxPriority == Int32.MaxValue)
                    {
                        break;
                    }
                }
            }

            // 发命令包
            ExecuteGGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            var ggEntry = m_gameGateInstances[gameGateServerId];
            if (gameEntry == null)
            {
                ggEntry.Status = ServerStatus.Inactive;
                req.Command = GateCommand.End;
                //req.TargetServerId = ServersMgr.Instance.ServerId;
                gameGateIdleQueue.Enqueue(gameGateServerId);
            }
            else
            {
                // 分配任务
                gameEntry.curGate.Add(gameGateServerId);
                ggEntry.curGameServerId = gameEntry.ServerInfo.ServerId;
                ggEntry.Status = ServerStatus.Active;
                req.Command = GateCommand.Start;
                //req.TargetServerId = ServersMgr.Instance.ServerId;
                req.GameServerInfoNode = gameEntry.ServerInfo;
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
            int maxPriority = Int32.MinValue;
            GameEntry gameEntry = null;
            foreach (var item in m_gameInstances)
            {
                if (item.Value.AssignScenePriority > maxPriority)
                {
                    maxPriority = item.Value.AssignScenePriority;
                    gameEntry = item.Value;
                    if (maxPriority == Int32.MaxValue)
                    {
                        break;
                    }
                }
            }

            // 发命令包
            ExecuteSCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            var sEntry = m_sceneInstances[sceneServerId];
            if (gameEntry == null)
            {
                sEntry.Status = ServerStatus.Inactive;
                sceneIdleQueue.Enqueue(sceneServerId);
                req.Command = GateCommand.End;
            }
            else
            {
                // 分配任务
                gameEntry.curScene.Add(sceneServerId);
                sEntry.curGameServerId = gameEntry.ServerInfo.ServerId;
                sEntry.Status = ServerStatus.Active;
                req.Command = GateCommand.Start;
                req.GameServerInfoNode = gameEntry.ServerInfo;
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
                for (int i = 0; i < entry.NeedGameGateCount - entry.curGate.Count; i++)
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
        private void ActiveGate(int gameGateServerId, int gameServerId)
        {
            GameEntry gEntry = m_gameInstances[gameServerId];
            GameGateEntry ggEntry = m_gameGateInstances[gameGateServerId];
            gEntry.curGate.Add(gameGateServerId);
            ggEntry.curGameServerId = gameServerId;
            ggEntry.Status = ServerStatus.Active;

            // 发包
            ExecuteGGCommandRequest req = new();
            req.TimeStamp = Scheduler.UnixTime;
            req.Command = GateCommand.Start;
            //req.TargetServerId = ServersMgr.Instance.ServerId;
            req.GameServerInfoNode = gEntry.ServerInfo;
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
            req.Command = GateCommand.Start;
            //req.TargetServerId = ServersMgr.Instance.ServerId;
            req.GameServerInfoNode = gEntry.ServerInfo;
            sEntry.Connection.Send(req);
        }

        public bool EntryDisconnection(int serverId)
        {
            if (m_gameGateInstances.ContainsKey(serverId))
            {
                Log.Information("a GameGateInstance disconnection, serverId = [{0}]", serverId);
                return _GameGateDisconnection(serverId);
            }
            else if (m_sceneInstances.ContainsKey(serverId))
            {
                Log.Information("a ScemeInstance disconnection, serverId = [{0}]", serverId);
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

        // tools
        private GameEntry GetGameEntryByWorldId(int worldId)
        {
            foreach(var entry in m_gameInstances.Values){
                if (entry.ServerInfo.GameServerInfo.GameWorldId == worldId) { 
                    return entry;
                }
            }
            return null;
        }
        public List<ServerInfoNode> RegisterSession(RegisterSessionToGGMRequest message)
        {
            List<ServerInfoNode> result = new();

            // 找对应的game对应轻松的gameGate返回回去,策略：
            // 1.随机选择：从可用的网关列表中随机选择一个。虽然简单，但能在一定程度上分摊负载。
            // 2.轮询（Round Robin）：按顺序循环选择下一个网关。这是一种经典的负载均衡方法，确保各个网关得到相等的请求量。
            // 3.最优选择：根据每个网关的当前负载或网络延迟情况选择最佳的网关。这需要对各个网关的状态进行监控，并可能需要额外的计算开销。
            // 4.权重轮询（Weighted Round Robin）：为每个网关设置一个权重，根据权重大小来分配连接请求。例如，性能更强的网关可以获得更高的权重。
            // 5.健康检查：定期检测各个网关的状态，仅选择那些健康的网关进行分发。
            GameEntry gameEntry = GetGameEntryByWorldId(message.WorldId);
            if(gameEntry == null)
            {
                goto End;
            }
            // 这里暂时使用轮询策略,并且直接分配两个gate
            int assignGateCnt = 2;
            List<GameGateEntry> list = new();
            if (gameEntry.curGate.Count <= assignGateCnt)
            {
                foreach(var gateServerId in gameEntry.curGate)
                {
                    list.Add(m_gameGateInstances[gateServerId]);
                }
                gameEntry.assignSessionIndex = 0;
            }
            else
            {
                int index = gameEntry.assignSessionIndex;
                for (int i = 0;i <assignGateCnt; ++i)
                {
                    list.Add(m_gameGateInstances[index]);
                    index = index % gameEntry.curGate.Count;
                }
                gameEntry.assignSessionIndex = index;
            }
            if(list.Count == 0)
            {
                goto End;
            }

            // 并且通知对应的gameGate设置session
            RegisterSessionToGGRequest req = new();
            req.SessionId = message.SessionId;
            req.UId = message.UId;
            foreach (var gateGateEntry in list) {
                gateGateEntry.Connection.Send(req);
                result.Add(gateGateEntry.ServerInfo);
            }
        End:
            return result;
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
