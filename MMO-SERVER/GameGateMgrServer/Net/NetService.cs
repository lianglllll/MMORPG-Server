using Serilog;
using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using System.Collections.Concurrent;
using HS.Protobuf.Common;
using static Common.Summer.Net.NetClient;
using System.Net;
using GameGateMgrServer.Utils;

namespace GameGateMgrServer.Net
{
    public class NetService : Singleton<NetService>
    {
        private TcpServer? m_acceptServer;
        private float m_heartBeatTimeOut;
        private ConcurrentDictionary<Connection, DateTime> m_serverConnHeartbeatTimestamps = new();
        private List<NetClient> m_outgoingServerConnection = new();

        public void Init()
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(Config.Server.workerCount);
            ProtoHelper.Instance.Init();
            // 协议注册
            ProtoHelper.Instance.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
            ProtoHelper.Instance.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<SSHeartBeatRequest>(_SSHeartBeatRequest);
            MessageRouter.Instance.Subscribe<SSHeartBeatResponse>(_SSHeartBeatResponse);
            // 定时发送ss心跳包
            Scheduler.Instance.AddTask(_SendSSHeatBeatReq, Config.Server.heartBeatSendInterval, 0);
        }
        public void Start()
        {
            _StartListeningForServerConnections();
            // 定时检查心跳包的情况
            m_heartBeatTimeOut = Config.Server.heartBeatTimeOut;
            Scheduler.Instance.AddTask(_CheckHeatBeat, Config.Server.heartBeatCheckInterval, 0);
        }
        public void UnInit()
        {
            m_acceptServer?.UnInit();
            m_acceptServer = null;
            m_serverConnHeartbeatTimestamps.Clear();
        }

        // GameGate、scene 或者 login 服务器连接过来的
        private void _StartListeningForServerConnections()
        {
            Log.Information("Starting to listen for serverConnections.{0}:{1}", Config.Server.ip, Config.Server.port);
            // 启动网络监听
            m_acceptServer = new TcpServer();
            m_acceptServer.Init(Config.Server.ip, Config.Server.port, 100, _HandleServerConnected, _HandleServerDisconnected);
        }
        private void _HandleServerConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Debug("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    // 给conn添加心跳时间
                    m_serverConnHeartbeatTimestamps[conn] = DateTime.Now;
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
        private void _HandleServerDisconnected(Connection conn)
        {
            //从心跳字典中删除连接
            if (m_serverConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_serverConnHeartbeatTimestamps.TryRemove(conn, out _);
            }

            // 通知上层删除
            int serverId = conn.Get<int>();
            if(serverId != 0)
            {
                Core.GGMMonitor.Instance.EntryDisconnection(serverId);
            }
        }
        private void _SSHeartBeatRequest(Connection conn, SSHeartBeatRequest message)
        {
            //更新心跳时间
            m_serverConnHeartbeatTimestamps[conn] = DateTime.Now;

            //响应
            SSHeartBeatResponse resp = new();
            conn.Send(resp);
        }
        private void _CheckHeatBeat()
        {
            DateTime nowTime = DateTime.Now;

            //这里规定心跳包超过m_lastHeartbeatTimes秒没用更新就将连接清理

            foreach (var kv in m_serverConnHeartbeatTimestamps)
            {
                TimeSpan gap = nowTime - kv.Value;
                if (gap.TotalSeconds > m_heartBeatTimeOut)
                {
                    //关闭超时的客户端连接
                    Connection conn = kv.Key;
                    //Log.Information("[心跳检查]心跳超时==>");//移除相关的资源
                    _CloseServerConnection(conn);
                }
            }

        }
        private void _CloseServerConnection(Connection conn)
        {
            if (conn == null) return;

            //从心跳字典中删除连接
            if (m_serverConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_serverConnHeartbeatTimestamps.TryRemove(conn, out _);
            }

            //转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }


        // GameGateMgrServer连接到其他服务器
        public NetClient ConnctToServer(string ip, int port,
            TcpClientConnectedCallback connected, TcpClientConnectedFailedCallback connectFailed,
            TcpClientDisconnectedCallback disconnected)
        {
            NetClient tcpClient = new NetClient();
            connected += OutgoingServerConnectionConnected;
            disconnected += OutgoingServerConnectionDisconnected;
            tcpClient.Init(ip, port, 10, connected, connectFailed, disconnected);
            return tcpClient;
        }
        private void OutgoingServerConnectionConnected(NetClient tcpClient)
        {
            m_outgoingServerConnection.Add(tcpClient);
        }
        private void OutgoingServerConnectionDisconnected(NetClient tcpClient)
        {
            m_outgoingServerConnection.Remove(tcpClient);
        }
        private void _SendSSHeatBeatReq()
        {
            foreach (var v in m_outgoingServerConnection)
            {
                SSHeartBeatRequest req = new SSHeartBeatRequest();
                v.Send(req);
            }
        }
        private void _SSHeartBeatResponse(Connection conn, SSHeartBeatResponse message)
        {
            // 知道对端也活着，嘻嘻。
        }
    }
}
