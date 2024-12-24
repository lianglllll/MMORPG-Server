using System.Net;
using Serilog;
using Common.Summer.Tools;
using Common.Summer.Net;
using Common.Summer.Core;
using ControlCenter.Utils;
using HS.Protobuf.Common;
using Common.Summer.Proto;
using HS.Protobuf.ControlCenter;

namespace ControlCenter.Net
{
    /// <summary>
    /// 网络服务
    /// </summary>
    public class NetService : Singleton<NetService>
    {
        private TcpServer m_tcpServer;
        private float m_heartBeatTimeOut;
        private Dictionary<Connection, DateTime> m_lastHeartbeatTimes = new();
        private Dictionary<int, ServerInfoNode> m_servers = new();
        private IdGenerator idGenerator = new();

        public void Init()
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(Config.Server.workerCount);
            ProtoHelper.Init();

            _RegisterProto();

            // 启动网络监听
            m_tcpServer = new TcpServer();
            m_tcpServer.Init(Config.Server.ip, Config.Server.port, 100, _OnConnected, _OnDisconnected);

            // 定时检查心跳包的情况
            m_heartBeatTimeOut = Config.Server.heartBeatTimeOut;
            Scheduler.Instance.AddTask(_CheckHeatBeat, Config.Server.heartBeatCheckInterval, 0);
        }
        public void UnInit()
        {
            m_tcpServer.UnInit();
            m_tcpServer = null;
            m_lastHeartbeatTimes.Clear();
        }
        private bool _RegisterProto()
        {
            // proto注册
            ProtoHelper.Register<HeartBeatRequest>((int)CommonProtocl.HeartbeatReq);
            ProtoHelper.Register<HeartBeatResponse>((int)CommonProtocl.HeartbeatResp);
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<HeartBeatRequest>(_HeartBeatRequest);
            MessageRouter.Instance.Subscribe<ServerInfoRegisterRequest>(_ServerInfoRegisterRequest);

            return true;
        }

        private void _OnConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Debug("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    // 给conn添加心跳时间
                    m_lastHeartbeatTimes[conn] = DateTime.Now;
                }
                else
                {
                    Log.Warning("[NetService]尝试访问已关闭的 Socket 对象");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.Error("[NetService]Socket 已被释放: " + ex.Message);
            }
        }
        private void _OnDisconnected(Connection conn)
        {
            if (conn == null) return;

            //从心跳字典中删除连接
            if (m_lastHeartbeatTimes.ContainsKey(conn))
            {
                m_lastHeartbeatTimes.Remove(conn);
            }

            int serverId = conn.Get<int>();
            if (serverId != 0 && m_servers.ContainsKey(serverId))
            {
                m_servers.Remove(serverId);
            }
        }
        public void CloseConnection(Connection conn)
        {
            if (conn == null) return;

            //从心跳字典中删除连接
            if (m_lastHeartbeatTimes.ContainsKey(conn))
            {
                m_lastHeartbeatTimes.Remove(conn);
            }

            int serverId = conn.Get<int>();
            if (serverId != 0 && m_servers.ContainsKey(serverId))
            {
                m_servers.Remove(serverId);
            }

            //转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }

        public void _HeartBeatRequest(Connection conn, HeartBeatRequest message)
        {
            //更新心跳时间
            m_lastHeartbeatTimes[conn] = DateTime.Now;

            //响应
            HeartBeatResponse resp = new HeartBeatResponse();
            conn.Send(resp);
        }
        private void _CheckHeatBeat()
        {
            Log.Debug($"心跳检测，当前连接数：{m_lastHeartbeatTimes.Count}");
            DateTime nowTime = DateTime.Now;
            //这里规定心跳包超过m_lastHeartbeatTimes秒没用更新就将连接清理
            foreach (var kv in m_lastHeartbeatTimes)
            {
                TimeSpan gap = nowTime - kv.Value;
                if (gap.TotalSeconds > m_heartBeatTimeOut)
                {
                    //关闭超时的客户端连接
                    Connection conn = kv.Key;
                    Log.Debug("心跳超时");//移除相关的资源
                    CloseConnection(conn);
                }
            }
        }
        private void _ServerInfoRegisterRequest(Connection conn, ServerInfoRegisterRequest message)
        {
            ServerInfoRegisterResponse resp = new();

            switch (message.ServerInfoNode.ServerType)
            {
                case SERVER_TYPE.Login:
                    break;
                case SERVER_TYPE.Logingate:
                    break;
                case SERVER_TYPE.Logingatemgr:
                    break;
                case SERVER_TYPE.Game:
                    break;
                case SERVER_TYPE.Gamegate:
                    break;
                case SERVER_TYPE.Gamegatemgr:
                    break;
                case SERVER_TYPE.Scene:
                    break;
                default:
                    Log.Error("NetService._ServerInfoRegisterRequest 错误的注册类型");
                    resp.ResultCode = 1;
                    resp.ResultMsg = "错误的注册类型";
                    goto End;
            }
            int serverId = idGenerator.GetId();
            m_servers.Add(serverId, message.ServerInfoNode);
            conn.Set<int>(serverId);//方便conn断开时找
            resp.ResultCode = 0;
            resp.ResultMsg = "注册成功";
            resp.ServerId = serverId;

        End:
            conn.Send(resp);
        }

        // test
        public Dictionary<int, ServerInfoNode> get()
        {
            return m_servers;
        }
    }
}
