using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
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
        private Dictionary<int, ServerInfoEntry> m_servers = new();
        private IdGenerator idGenerator = new();

        public bool Init()
        {
            NetService.Instance.Init();
            ControlCenterHandler.Instance.Init();

            // proto注册
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterRequest>(_ServerInfoRegisterRequest);

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
        private void _ServerInfoRegisterRequest(Connection conn, ServerInfoRegisterRequest message)
        {
            ServerInfoRegisterResponse resp = new();
            ClusterEventResponse eventResp = new();

            switch (message.ServerInfoNode.ServerType)
            {
                case SERVER_TYPE.Login:
                    eventResp.EventType = ClusterEventType.LoginEnter;
                    break;
                case SERVER_TYPE.Logingate:
                    eventResp.EventType = ClusterEventType.LogingateEnter;
                    break;
                case SERVER_TYPE.Logingatemgr:
                    eventResp.EventType = ClusterEventType.LogingatemgrEnter;
                    break;
                case SERVER_TYPE.Game:
                    eventResp.EventType = ClusterEventType.GameEnter;
                    break;
                case SERVER_TYPE.Gamegate:
                    eventResp.EventType = ClusterEventType.GamegateEnter;
                    break;
                case SERVER_TYPE.Gamegatemgr:
                    eventResp.EventType = ClusterEventType.GamegatemgrEnter;
                    break;
                case SERVER_TYPE.Scene:
                    eventResp.EventType = ClusterEventType.SceneEnter;
                    break;
                case SERVER_TYPE.Dbproxy:
                    eventResp.EventType = ClusterEventType.DbproxyEnter;
                    break;
                default:
                    Log.Error("NetService._ServerInfoRegisterRequest 错误的注册类型");
                    resp.ResultCode = 1;
                    resp.ResultMsg = "错误的注册类型";
                    goto End;
            }
            int serverId = idGenerator.GetId();
            ServerInfoNode node = message.ServerInfoNode;
            node.ServerId = serverId;
            conn.Set<int>(serverId);//方便conn断开时找
            m_servers.Add(serverId, new ServerInfoEntry() { Connection = conn, ServerInfoNode = node });

            resp.ResultCode = 0;
            resp.ResultMsg = "注册成功";
            resp.ServerId = serverId;
            Log.Information($"[ServerInfoRegister] 注册成功，{node.ToString()}");

            // 事件通知和分发
            eventResp.ServerId = serverId;
            eventResp.ServerInfoNode = node;
            _EventDispatch(eventResp);


        End:
            conn.Send(resp);
        }
        private bool _EventDispatch(ClusterEventResponse eventResp)
        {
            foreach(var entry in m_servers.Values)
            {
                int bitmap = entry.ServerInfoNode.EventBitmap;
                if ((bitmap & (1 << (int)eventResp.EventType)) != 0)
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
