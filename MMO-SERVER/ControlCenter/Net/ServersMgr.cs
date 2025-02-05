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
            m_serversByType[SERVER_TYPE.Login]          = new();
            m_serversByType[SERVER_TYPE.Logingate]      = new();
            m_serversByType[SERVER_TYPE.Logingatemgr]   = new();
            m_serversByType[SERVER_TYPE.Game]           = new();
            m_serversByType[SERVER_TYPE.Gamegate]       = new();
            m_serversByType[SERVER_TYPE.Scene]          = new();
            m_serversByType[SERVER_TYPE.Gamegatemgr]    = new();
            m_serversByType[SERVER_TYPE.Dbproxy]        = new();



            NetService.Instance.Init();
            ControlCenterHandler.Instance.Init();

            // proto注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterRequest>(_HandleServerInfoRegisterRequest);

            // 获取持久化的serverinfo,因为本次启动可能是宕机之后起来的。
            //StaticDataManager.Instance.Init();
            //var node = new ServerInfoNode();
            //node.Ip = "123123";
            //node.Port = 1234;
            //node.ServerType = SERVER_TYPE.Game;
            //node.GameServerInfo = new GameServerInfoNode
            //{
            //    GameWorldId = 1
            //};
            //StaticDataManager.Instance.serverInfoNodeDict.Add(1, node);
            //StaticDataManager.Instance.Save("test.json");
            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        // register || unregister
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
                    Log.Error("NetService._ServerInfoRegisterRequest 错误的注册类型:{0}", message.ServerInfoNode.ServerType.ToString());
                    resp.ResultCode = 1;
                    resp.ResultMsg = "错误的注册类型";
                    goto End;
            }
            int newServerId = idGenerator.GetId();
            conn.Set<int>(newServerId);//方便conn断开时找
            
            ServerInfoNode newNode = message.ServerInfoNode;
            newNode.ServerId = newServerId;
            m_servers.Add(newServerId, new ServerInfoEntry() { Connection = conn, ServerInfoNode = newNode });
            m_serversByType[newNode.ServerType].Add(newServerId);

            // 回包
            resp.ResultCode = 0;
            resp.ResultMsg = "注册成功";
            resp.ServerId = newServerId;
            List<ClusterEventNode> clusterEventNodes = _DispatchMissedEvents(newServerId);
            resp.ClusterEventNodes.AddRange(clusterEventNodes);
            Log.Information("Successfully registered serverInfo, {0}", newNode.ToString());

            // 对之前已经存在的server进行事件通知和分发
            ceNode.ServerId = newServerId;
            ceNode.ServerInfoNode = newNode;
            _DispatchEvent(cEventResp);

        End:
            conn.Send(resp);
        }
        public bool HaveInstanceDisconnected(int serverId)
        {
            if (m_servers.ContainsKey(serverId))
            {
                Log.Error("Server instance disconnect, serverId = {0}", serverId);

                var serverInfoNode = m_servers[serverId].ServerInfoNode;
                ClusterEventResponse cEventResp = new();
                ClusterEventNode ceNode = new();
                cEventResp.ClusterEventNode = ceNode;
                ceNode.ServerId = serverId;
                switch (serverInfoNode.ServerType)
                {
                    case SERVER_TYPE.Login:
                        ceNode.EventType = ClusterEventType.LoginExit;
                        break;
                    case SERVER_TYPE.Logingate:
                        ceNode.EventType = ClusterEventType.LogingateExit;
                        break;
                    case SERVER_TYPE.Logingatemgr:
                        ceNode.EventType = ClusterEventType.LogingatemgrExit;
                        break;
                    case SERVER_TYPE.Game:
                        ceNode.EventType = ClusterEventType.GameExit;
                        break;
                    case SERVER_TYPE.Gamegate:
                        ceNode.EventType = ClusterEventType.GamegateExit;
                        break;
                    case SERVER_TYPE.Gamegatemgr:
                        ceNode.EventType = ClusterEventType.GamegatemgrExit;
                        break;
                    case SERVER_TYPE.Scene:
                        ceNode.EventType = ClusterEventType.SceneExit;
                        break;
                    case SERVER_TYPE.Dbproxy:
                        ceNode.EventType = ClusterEventType.DbproxyExit;
                        break;
                    default:
                        Log.Error("NetService._ServerInfoRegisterRequest 错误的断开类型 = {0}", serverInfoNode.ServerType.ToString());
                        break;
                }

                m_serversByType[serverInfoNode.ServerType].Remove(serverId);
                m_servers.Remove(serverId);
                idGenerator.ReturnId(serverId);

                _DispatchEvent(cEventResp);
            End:
                return true;
            }
            return false;
        }

        // event
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

        // tools
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
