using System;
using System.Net;
using Serilog;
using System.Threading;
using GameServer.Utils;
using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using System.Collections.Concurrent;
using HS.Protobuf.Common;
using static Common.Summer.Net.NetClient;
using System.Collections.Generic;

namespace GameServer.Net
{
    public class NetService : Singleton<NetService>
    {
        private TcpServer m_acceptServer;
        private float m_heartBeatTimeOut;
        private ConcurrentDictionary<Connection, DateTime> m_serverConnHeartbeatTimestamps = new();
        private List<NetClient> m_outgoingServerConnection = new();

        public void Init()
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(Config.Server.workerCount);
            ProtoHelper.Instance.Init();
            // 协议注册
            ProtoHelper.Instance.Register<CSHeartBeatRequest>((int)CommonProtocl.CsHeartbeatReq);
            ProtoHelper.Instance.Register<CSHeartBeatResponse>((int)CommonProtocl.CsHeartbeatResp);
            ProtoHelper.Instance.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
            ProtoHelper.Instance.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<SSHeartBeatRequest>(_HandleSSHeartBeatRequest);
            MessageRouter.Instance.Subscribe<SSHeartBeatResponse>(_HandleSSHeartBeatResponse);
            // 定时发送ss心跳包
            Scheduler.Instance.AddTask(_SendSSHeatBeatReq, Config.Server.heartBeatSendInterval, 0);
        }
        public void Init2()
        {
            _StartListeningForServerConnections();
            // 定时检查心跳包的情况
            m_heartBeatTimeOut = Config.Server.heartBeatTimeOut;
            Scheduler.Instance.AddTask(_CheckHeatBeat, Config.Server.heartBeatCheckInterval, 0);
        }
        public void UnInit()
        {
            m_acceptServer.UnInit();
            m_acceptServer = null;
            m_serverConnHeartbeatTimestamps.Clear();
        }


        // 1.其他服务器连接过来的（gate、scene）
        private void _StartListeningForServerConnections()
        {
            Log.Information("Starting to listen for serverConnections.");
            // 启动网络监听
            m_acceptServer = new TcpServer();
            m_acceptServer.Init(Config.Server.ip, Config.Server.serverPort, 100, _HandleServerConnected, _HandleServerDisconnected);
        }
        private void _HandleServerConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Debug("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    AllocateConnectionResource(conn);
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
        }
        public void CloseServerConnection(Connection conn)
        {
            // 清理资源
            CleanConnectionResource(conn);
            // 转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }
        private void AllocateConnectionResource(Connection conn)
        {
            // 给conn添加心跳时间
            m_serverConnHeartbeatTimestamps[conn] = DateTime.Now;

        }
        private void CleanConnectionResource(Connection conn)
        {
            // 从心跳字典中删除连接
            if (m_serverConnHeartbeatTimestamps.ContainsKey(conn))
            {
                m_serverConnHeartbeatTimestamps.TryRemove(conn, out _);
            }
        }
        private void _HandleSSHeartBeatRequest(Connection conn, SSHeartBeatRequest message)
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
                    CloseServerConnection(conn);
                }
            }

        }


        // 2.服务器主动连接其他的服务器
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
        private void _HandleSSHeartBeatResponse(Connection conn, SSHeartBeatResponse message)
        {
            // 知道对端也活着，嘻嘻。
        }

        #region 旧代码
        /*
        private TcpServer m_acceptUser;
        private ConcurrentDictionary<Connection, DateTime> m_userConnHeartbeatTimestamps = new();

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
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Information("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());

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

            //session
            var session = conn.Get<Session>();
            if (session != null)
            {
                session.Conn = null;
            }

            //测试信息
            if (session != null)
            {
                Log.Information("[连接断开]用户名：" + session.dbUser.Username);
            }
            else
            {
                Log.Information("[连接断开]未知用户");
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

            //session
            var session = conn.Get<Session>();
            if (session != null)
            {
                session.Conn = null;
            }

            //转交给下一层的connection去进行关闭
            conn.CloseConnection();
        }
        private void _CSHeartBeatRequest(Connection conn, CSHeartBeatRequest message)
        {
            //更新心跳时间
            m_userConnHeartbeatTimestamps[conn] = DateTime.Now;

            //更新运行时间
            var session = conn.Get<Session>();
            if (session != null)
            {
                session.LastHeartTime = MyTime.time;
            }

            //响应
            CSHeartBeatResponse resp = new CSHeartBeatResponse();
            conn.Send(resp);
        }
        */
        #endregion
    }
}
