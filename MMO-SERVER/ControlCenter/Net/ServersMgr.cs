using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using ControlCenter.Net;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;

namespace ControlCenter.Core
{
    class ServerInfoEntry
    {
        public Connection Connection { get; set; }
        public ServerInfoNode ServerInfoNode { get; set; }
    }

    public class ServersMgr : Singleton<ServersMgr>
    {
        private IdGenerator idGenerator = new();
        private Dictionary<int, ServerInfoEntry> m_servers = new();
        private Dictionary<SERVER_TYPE, List<int>> m_serversByType = new();

        public bool Init()
        {
            m_serversByType[SERVER_TYPE.Login] = new List<int>();
            m_serversByType[SERVER_TYPE.Logingate] = new List<int>();
            m_serversByType[SERVER_TYPE.Logingatemgr] = new List<int>();
            m_serversByType[SERVER_TYPE.Game] = new List<int>();
            m_serversByType[SERVER_TYPE.Gamegate] = new List<int>();
            m_serversByType[SERVER_TYPE.Gamegatemgr] = new List<int>();
            m_serversByType[SERVER_TYPE.Dbproxy] = new List<int>();
            m_serversByType[SERVER_TYPE.Scene] = new List<int>();

            NetService.Instance.Init();
            ControlCenterHandler.Instance.Init();

            // proto注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterRequest>(_HandleServerInfoRegisterRequest);

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public bool OnDisconnected(int serverId)
        {
            if (m_servers.ContainsKey(serverId))
            {
                m_servers.Remove(serverId);
                idGenerator.ReturnId(serverId);
                return true;
            }
            return false;
        }
        private void _HandleServerInfoRegisterRequest(Connection conn, ServerInfoRegisterRequest message)
        {
            ServerInfoRegisterResponse resp = new();
            ClusterEventResponse cEventResp = new();
            ClusterEventNode ceNode = new();
            cEventResp.ClusterEventNode = ceNode;

            switch (message.ServerInfoNode.ServerType)
            {
                case SERVER_TYPE.Login:
                    ceNode.EventType = ClusterEventType.LoginEnter;
                    break;
                case SERVER_TYPE.Logingate:
                    ceNode.EventType = ClusterEventType.LogingateEnter;
                    break;
                case SERVER_TYPE.Logingatemgr:
                    ceNode.EventType = ClusterEventType.LogingatemgrEnter;
                    break;
                case SERVER_TYPE.Game:
                    ceNode.EventType = ClusterEventType.GameEnter;
                    break;
                case SERVER_TYPE.Gamegate:
                    ceNode.EventType = ClusterEventType.GamegateEnter;
                    break;
                case SERVER_TYPE.Gamegatemgr:
                    ceNode.EventType = ClusterEventType.GamegatemgrEnter;
                    break;
                case SERVER_TYPE.Scene:
                    ceNode.EventType = ClusterEventType.SceneEnter;
                    break;
                case SERVER_TYPE.Dbproxy:
                    ceNode.EventType = ClusterEventType.DbproxyEnter;
                    break;
                default:
                    Log.Error("NetService._ServerInfoRegisterRequest 错误的注册类型");
                    resp.ResultCode = 1;
                    resp.ResultMsg = "错误的注册类型";
                    goto End;
            }
            ServerInfoNode node = message.ServerInfoNode;
            int serverId = idGenerator.GetId();
            node.ServerId = serverId;
            conn.Set<int>(serverId);//方便conn断开时找
            m_servers.Add(serverId, new ServerInfoEntry() { Connection = conn, ServerInfoNode = node });
            m_serversByType[node.ServerType].Add(serverId);

            // 对之前已经存在的server进行事件通知和分发
            ceNode.ServerId = serverId;
            ceNode.ServerInfoNode = node;
            _DispatchEvent(cEventResp);

            resp.ResultCode = 0;
            resp.ResultMsg = "注册成功";
            resp.ServerId = serverId;
            List<ClusterEventNode> clusterEventNodes = _DispatchMissedEvents(serverId);
            resp.ClusterEventNodes.AddRange(clusterEventNodes);
            Log.Information($"[ServerInfoRegister] 注册成功，{node.ToString()}");
        End:
            conn.Send(resp);
        }
        private List<ClusterEventNode> _DispatchMissedEvents(int serverId)
        {
            // 在当前server注册之前可能已经有它关心的事件发生了，我们需要补发给它

            List<ClusterEventNode> clusterEventNodes = new List<ClusterEventNode>();
            ServerInfoEntry sEntry = m_servers[serverId];
            int bitMap = sEntry.ServerInfoNode.EventBitmap;

            for (int i = 0; i < 32; i++)
            {
                if ((bitMap & (1 << i)) != 0)
                {
                    switch (i)
                    {
                        case (int)ClusterEventType.LoginEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Login, ClusterEventType.LoginEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.LogingateEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Logingate, ClusterEventType.LogingateEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.LogingatemgrEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Logingatemgr, ClusterEventType.LogingatemgrEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.GameEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Game, ClusterEventType.GameEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.GamegateEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Gamegate, ClusterEventType.GamegateEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.GamegatemgrEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Gamegatemgr, ClusterEventType.GamegatemgrEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.SceneEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Scene, ClusterEventType.SceneEnter, clusterEventNodes);
                            break;
                        case (int)ClusterEventType.DbproxyEnter:
                            _AddClusterEventNodes(SERVER_TYPE.Dbproxy, ClusterEventType.DbproxyEnter, clusterEventNodes);
                            break;
                        // 可以添加更多进入事件的处理
                        default:
                            break;
                    }
                }
            }
            return clusterEventNodes;
        }
        private void _AddClusterEventNodes(SERVER_TYPE serverType, ClusterEventType eventType, List<ClusterEventNode> clusterEventNodes)
        {
            var ids = m_serversByType[serverType];
            foreach (var id in ids)
            {
                var node = new ClusterEventNode
                {
                    ServerId = id,
                    EventType = eventType,
                    ServerInfoNode = m_servers[id].ServerInfoNode
                };
                clusterEventNodes.Add(node);
            }
        }
        private bool _DispatchEvent(ClusterEventResponse eventResp)
        {
            foreach(var entry in m_servers.Values)
            {
                int bitmap = entry.ServerInfoNode.EventBitmap;
                if ((bitmap & (1 << (int)eventResp.ClusterEventNode.EventType)) != 0)
                {
                    entry.Connection.Send(eventResp);
                }
            }
            return true;
        }

        public List<ServerInfoNode> GetAllServerInfoByServerType(SERVER_TYPE sERVER_TYPE)
        {
            List<ServerInfoNode> serverInfoNodes = new();
            foreach (var item in m_servers)
            {
                if (item.Value.ServerInfoNode.ServerType == sERVER_TYPE)
                {
                    serverInfoNodes.Add(item.Value.ServerInfoNode);
                }
            }
            return serverInfoNodes;
        }

    }
}
