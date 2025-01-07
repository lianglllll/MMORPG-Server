using Serilog;
using GameGateServer.Utils;
using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using System.Collections.Concurrent;
using HS.Protobuf.Common;
using static Common.Summer.Net.NetClient;

namespace GameGateServer.Net
{
    public class NetService : Singleton<NetService>
    {
        private TcpServer? m_acceptUser;
        private float m_heartBeatTimeOut;
        private ConcurrentDictionary<Connection, DateTime> m_userConnHeartbeatTimestamps = new();
        private ConcurrentDictionary<Connection, DateTime> m_serverConnHeartbeatTimestamps = new();
        private List<NetClient> m_outgoingServerConnection = new();

        public void Init()
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(Config.Server.workerCount);
            ProtoHelper.Init();
            // 协议注册
            ProtoHelper.Register<CSHeartBeatRequest>((int)CommonProtocl.CsHeartbeatReq);
            ProtoHelper.Register<CSHeartBeatResponse>((int)CommonProtocl.CsHeartbeatResp);
            ProtoHelper.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
            ProtoHelper.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<CSHeartBeatRequest>(_CSHeartBeatRequest);
            MessageRouter.Instance.Subscribe<SSHeartBeatResponse>(_SSHeartBeatResponse);
            // 定时发送ss心跳包
            Scheduler.Instance.AddTask(_SendSSHeatBeatReq, Config.Server.heartBeatSendInterval, 0);
        }

        public void UnInit()
        {
            m_acceptUser?.UnInit();
            m_acceptUser = null;
            m_userConnHeartbeatTimestamps.Clear();
            m_serverConnHeartbeatTimestamps.Clear();
        }
        public void Start()
        {
            _StartListeningForUserConnections();
            // 定时检查心跳包的情况
            m_heartBeatTimeOut = Config.Server.heartBeatTimeOut;
            Scheduler.Instance.AddTask(_CheckHeatBeat, Config.Server.heartBeatCheckInterval, 0);
        }
        public void Stop()
        {
            m_acceptUser?.Stop();
        }
        public void Resume()
        {
            m_acceptUser?.Resume();
        }

        // 1.用户连接过来的
        private void _StartListeningForUserConnections()
        {
            Log.Information("Starting to listen for userConnections.");
            // 启动网络监听
            m_acceptUser = new TcpServer();
            m_acceptUser.Init(Config.Server.ip, Config.Server.userPort, 100, _HandleUserConnected, _HandleClientDisconnected);
        }
        private void _HandleUserConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    //var ipe = conn.Socket.RemoteEndPoint;
                    //Log.Information("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    
                    // 给conn添加心跳时间
                    m_userConnHeartbeatTimestamps[conn] = DateTime.Now;
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
        private void _HandleClientDisconnected(Connection conn)
        {
            //从心跳字典中删除连接
            if (m_userConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_userConnHeartbeatTimestamps.TryRemove(conn,out _);
            }
        }
        public void CloseUserConnection(Connection conn)
        {
            if (conn == null) return;

            //从心跳字典中删除连接
            if (m_userConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_userConnHeartbeatTimestamps.TryRemove(conn, out _);
            }

            //转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }
        private void _CSHeartBeatRequest(Connection conn, CSHeartBeatRequest message)
        {
            //更新心跳时间
            m_userConnHeartbeatTimestamps[conn] = DateTime.Now;

            //响应
            CSHeartBeatResponse resp = new CSHeartBeatResponse();
            conn.Send(resp);
        }
        private void _CheckHeatBeat()
        {
            DateTime nowTime = DateTime.Now;

            //这里规定心跳包超过m_lastHeartbeatTimes秒没用更新就将连接清理
            foreach (var kv in m_userConnHeartbeatTimestamps)
            {
                TimeSpan gap = nowTime - kv.Value;
                if (gap.TotalSeconds > m_heartBeatTimeOut)
                {
                    //关闭超时的客户端连接
                    Connection conn = kv.Key;
                    //Log.Information("[心跳检查]心跳超时==>");//移除相关的资源
                    CloseUserConnection(conn);
                }
            }
        }


        // 3.服务器主动连接其他的服务器
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
        public void CloseServerConnection(NetClient nc)
        {
            var conn = nc.CloseConnection();
            m_serverConnHeartbeatTimestamps.TryRemove(conn,out _);
        }
    }
}
