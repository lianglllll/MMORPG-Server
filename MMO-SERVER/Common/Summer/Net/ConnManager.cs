using Common.Summer.Core;
using Common.Summer.Tools;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using HS.Protobuf.Common;
using Serilog;
using System.Net;
using static Common.Summer.Net.NetClient;

namespace Common.Summer.Net
{
    // 连接管理器
    // 1.心跳的处理
    public class ConnManager : Singleton<ConnManager>
    {
        private float m_heartBeatSendInterval;
        private float m_heartBeatCheckInterval;
        private float m_heartBeatTimeOut;
        
        private bool m_isAcceptUser;
        private TcpServer m_userAcceptServer;
        private ConcurrentDictionary<Connection, DateTime> m_userConnHeartbeatTimestamps = new();
        private string m_acceptUserIp;
        private int m_acceptUserPort;
        Action<Connection> m_userConnectedAction;
        Action<Connection> m_userDisconnectedAction;

        private bool m_isAcceptServer;  
        private TcpServer m_serverAcceptServer;
        private ConcurrentDictionary<Connection, DateTime> m_serverConnHeartbeatTimestamps = new();
        private string m_acceptServerIp;
        private int m_acceptServerPort;
        Action<Connection> m_serverConnectedAction;
        Action<Connection> m_serverDisconnectedAction;

        private bool m_isConnectToOther;
        private List<NetClient> m_outgoingServerConnection = new();

        public void Init(int workerCount, float heartBeatSendInterval, float heartBeatCheckInterval, float heartBeatTimeOut, bool isAcceptUser, bool isAcceptServer, bool isConnectToOther,
            string acceptUserIp, int acceptUserPort, Action<Connection> userConnectedAction, Action<Connection> userDisconnectedAction,
            string acceptServerIp, int acceptServerPort, Action<Connection> serverConnectedAction, Action<Connection> serverDisconnectedAction)
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(workerCount);
            ProtoHelper.Instance.Init();

            m_heartBeatSendInterval = heartBeatSendInterval;
            m_heartBeatCheckInterval = heartBeatCheckInterval;
            m_heartBeatTimeOut = heartBeatTimeOut;

            m_isAcceptUser = isAcceptUser;
            m_isAcceptServer = isAcceptServer;
            m_isConnectToOther = isConnectToOther;

            if (isAcceptUser)
            {
                ProtoHelper.Instance.Register<CSHeartBeatRequest>((int)CommonProtocl.CsHeartbeatReq);
                ProtoHelper.Instance.Register<CSHeartBeatResponse>((int)CommonProtocl.CsHeartbeatResp);
                MessageRouter.Instance.Subscribe<CSHeartBeatRequest>(_HandleCSHeartBeatRequest);
                m_acceptUserIp = acceptUserIp;
                m_acceptUserPort = acceptUserPort;
                m_userConnectedAction = userConnectedAction;
                m_userDisconnectedAction = userDisconnectedAction;
            }
            if (isAcceptServer) {
                ProtoHelper.Instance.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
                ProtoHelper.Instance.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);
                MessageRouter.Instance.Subscribe<SSHeartBeatRequest>(_HandleSSHeartBeatRequest);
                m_acceptServerIp = acceptServerIp;
                m_acceptServerPort = acceptServerPort;
                m_serverConnectedAction = serverConnectedAction;
                m_serverDisconnectedAction = serverDisconnectedAction;
            }
            if (isConnectToOther)
            {
                ProtoHelper.Instance.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
                ProtoHelper.Instance.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);
                MessageRouter.Instance.Subscribe<SSHeartBeatResponse>(_HandleSSHeartBeatResponse);
                // 定时发送ss心跳包
                Scheduler.Instance.AddTask(_SendSSHeatBeatReq, heartBeatSendInterval, 0);
            }
        }
        public void Start()
        {
            if (m_isAcceptUser)
            {
                _StartListeningForUserConnections(m_acceptUserIp, m_acceptUserPort);
                Scheduler.Instance.AddTask(_CheckCSHeatBeat, m_heartBeatCheckInterval, 0);
            }
            if (m_isAcceptServer)
            {
                _StartListeningForClusterServerConnections(m_acceptServerIp, m_acceptServerPort);
                Scheduler.Instance.AddTask(_CheckSSHeatBeat, m_heartBeatCheckInterval, 0);
            }
        }
        public void UserStart()
        {
            _StartListeningForUserConnections(m_acceptUserIp, m_acceptUserPort);
        }
        public void UserEnd()
        {
            // 移除全部的user连接
            foreach (var kv in m_userConnHeartbeatTimestamps)
            {
                Connection conn = kv.Key;
                CloseUserConnection(conn);
            }
            m_userConnHeartbeatTimestamps.Clear();
            m_userAcceptServer?.UnInit();
            m_userAcceptServer = null;
        }
        public void ServerStart()
        {
            _StartListeningForClusterServerConnections(m_acceptUserIp, m_acceptUserPort);
        }
        public void ServerEnd()
        {
            // 移除全部的user连接
            foreach (var kv in m_serverConnHeartbeatTimestamps)
            {
                Connection conn = kv.Key;
                CloseServerConnection(conn);
            }
            m_serverConnHeartbeatTimestamps.Clear();
            m_serverAcceptServer?.UnInit();
            m_serverAcceptServer = null;
        }




        // 1.用户连接过来的
        private void _StartListeningForUserConnections(string ip, int port)
        {
            Log.Information("Starting to listen for userConnections[{0}:{1}].", ip, port);
            // 启动网络监听
            m_userAcceptServer = new TcpServer();
            m_userAcceptServer.Init(ip, port, 100, _HandleUserConnected, _HandleUserDisconnected);
        }
        private void _HandleUserConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Information("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    // 给conn添加心跳时间
                    m_userConnHeartbeatTimestamps[conn] = DateTime.Now;
                    // 通知上层
                    m_userConnectedAction?.Invoke(conn);
                }
                else
                {
                    Log.Warning("[ConnManager]尝试访问已关闭的 Socket 对象");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.Error("[ConnManager]Socket 已被释放: " + ex.Message);
            }
        }
        private void _HandleUserDisconnected(Connection conn)
        {
            Log.Debug("user断开连接...");
            // 从心跳字典中删除连接
            if (m_userConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_userConnHeartbeatTimestamps.TryRemove(conn, out _);
            }
            // 通知上层
            m_userDisconnectedAction?.Invoke(conn);
        }
        private void _CheckCSHeatBeat()
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
                    Log.Information("[心跳检查]心跳超时==>");

                    m_userDisconnectedAction?.Invoke(conn);
                    CloseUserConnection(conn);
                }
            }
        }
        public void CloseUserConnection(Connection conn)
        {
            if (conn == null) return;

            // 从心跳字典中删除连接
            if (m_userConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_userConnHeartbeatTimestamps.TryRemove(conn, out _);
            }
            //转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }
        private void _HandleCSHeartBeatRequest(Connection conn, CSHeartBeatRequest message)
        {
            //更新心跳时间
            m_userConnHeartbeatTimestamps[conn] = DateTime.Now;

            //响应
            CSHeartBeatResponse resp = new CSHeartBeatResponse();
            conn.Send(resp);
        }


        // 2.分布式系统中其他服务器连接过来的
        private void _StartListeningForClusterServerConnections(string ip, int port)
        {
            Log.Information("Starting to listen for serverConnections[{0}:{1}].",ip, port);
            // 启动网络监听
            m_serverAcceptServer = new TcpServer();
            m_serverAcceptServer.Init(ip, port, 100, _HandleClusterServerConnected, _HandleClusterServerDisconnected);
        }
        private void _HandleClusterServerConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Debug("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    // 给conn添加心跳时间
                    m_serverConnHeartbeatTimestamps[conn] = DateTime.Now;
                    // 通知上层
                    m_serverConnectedAction?.Invoke(conn);
                }
                else
                {
                    Log.Warning("[ConnManager]尝试访问已关闭的 Socket 对象");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.Error("[ConnManager]Socket 已被释放: " + ex.Message);
            }
        }
        private void _HandleClusterServerDisconnected(Connection conn)
        {
            Log.Debug("server断开连接...");
            //从心跳字典中删除连接
            if (m_serverConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_serverConnHeartbeatTimestamps.TryRemove(conn, out _);
            }
            // 通知上层
            m_serverDisconnectedAction?.Invoke(conn);
        }
        private void _CheckSSHeatBeat()
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
                    Log.Debug("[心跳检查]心跳超时==>");

                    m_serverDisconnectedAction?.Invoke(conn);
                    CloseServerConnection(conn);
                }
            }

        }
        public void CloseServerConnection(Connection conn)
        {
            // 从心跳字典中删除连接
            if (m_serverConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_serverConnHeartbeatTimestamps.TryRemove(conn, out _);
            }
            // 转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }
        private void _HandleSSHeartBeatRequest(Connection conn, SSHeartBeatRequest message)
        {
            //更新心跳时间
            m_serverConnHeartbeatTimestamps[conn] = DateTime.Now;

            //响应
            SSHeartBeatResponse resp = new();
            conn.Send(resp);
        }


        // 3.本服务器连接到分布式系统中其他服务器
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
        public void CloseOutgoingServerConnection(NetClient nc)
        {
            m_outgoingServerConnection.Remove(nc);
            nc.CloseConnection();
        }
        private void _SendSSHeatBeatReq()
        {
            foreach (var v in m_outgoingServerConnection)
            {
                SSHeartBeatRequest req = new SSHeartBeatRequest();
                v.Send(req);
            }
        }
        private void _HandleSSHeartBeatResponse(Connection conn, SSHeartBeatResponse message)
        {
            // 知道对端也活着，嘻嘻。
        }
    }
}
