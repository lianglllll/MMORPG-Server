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
using Common.Summer.Proto;
using HS.Protobuf.ControlCenter;

namespace GameServer.Net
{
    /// <summary>
    /// 网络服务
    /// </summary>
    public class NetService : Singleton<NetService>
    {
        private int m_serverId;
        private TcpServer m_tcpServer;
        private ConcurrentDictionary<Connection, DateTime> m_lastHeartbeatTimes = new();
        private ConcurrentDictionary<SERVER_TYPE, TcpClient> m_curConnOtherServer = new();
        private float m_heartBeatTimeOut;

        public void Init()
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(Config.Server.workerCount);
            ProtoHelper.Init();

            RegisterProto();

            //连接controlcenter服务器
            TcpClient m_ccClient = new TcpClient();
            m_curConnOtherServer.TryAdd(SERVER_TYPE.Controlcenter, m_ccClient);
            m_ccClient.Init(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        public void UnInit()
        {
            m_tcpServer.UnInit();
            m_tcpServer = null;
            m_lastHeartbeatTimes.Clear();
            m_curConnOtherServer.Clear();
        }
        private bool RegisterProto()
        {
            // 协议注册
            ProtoHelper.Register<HeartBeatRequest>((int)CommonProtocl.HeartbeatReq);
            ProtoHelper.Register<HeartBeatResponse>((int)CommonProtocl.HeartbeatResp);
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<HeartBeatRequest>(_HeartBeatRequest);
            MessageRouter.Instance.Subscribe<HeartBeatResponse>(_HeartBeatResponse);
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_ServerInfoRegisterResponse);

            return true;
        }

        private void _CCConnectedCallback()
        {
            // 定时发送心跳包
            Scheduler.Instance.AddTask(_Send2OtherServerHeatBeatReq, Config.Server.heartBeatSendInterval, 0);

            //向cc注册自己
            ServerInfoRegisterRequest req = new();
            ServerInfoNode node = new();
            node.ServerType = SERVER_TYPE.Game;
            node.Ip = Config.Server.ip;
            node.Port = Config.Server.port;
            req.ServerInfoNode = node;
            m_curConnOtherServer[SERVER_TYPE.Controlcenter].Send(req);
        }
        private void _CCConnectedFailedCallback(bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to controlCenter failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to controlCenter failed, attempting to reconnect controlCenter");
            }

        }
        private void _CCDisconnectedCallback()
        {
            //尝试重连
        }
        private void _ServerInfoRegisterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if(message.ResultCode == 0)
            {
                m_serverId = message.ServerId;
                Log.Information("Successfully registered this server information with the ControlCenter.");
                Log.Information($"The server ID of this server is [{m_serverId}]");
                _StartListening();
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }

        private void _StartListening()
        {
            Log.Information("Starting to listen for connections.");
            // 启动网络监听
            m_tcpServer = new TcpServer();
            m_tcpServer.Init(Config.Server.ip, Config.Server.port, 100, _OnConnected, _OnDisconnected);

            // 定时检查心跳包的情况
            m_heartBeatTimeOut = Config.Server.heartBeatTimeOut;
            Timer timer = new Timer(_CheckHeatBeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(Config.Server.heartBeatCheckInterval));
        }
        private void _OnConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Information("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());

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
            //从心跳字典中删除连接
            if (m_lastHeartbeatTimes.ContainsKey(conn))
            {
                m_lastHeartbeatTimes.TryRemove(conn,out _);
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
        public void CloseConnection(Connection conn)
        {
            if (conn == null) return;

            //从心跳字典中删除连接
            if (m_lastHeartbeatTimes.ContainsKey(conn))
            {
                m_lastHeartbeatTimes.TryRemove(conn, out _);
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

        private void _HeartBeatRequest(Connection conn, HeartBeatRequest message)
        {
            //更新心跳时间
            m_lastHeartbeatTimes[conn] = DateTime.Now;

            //更新运行时间
            var session = conn.Get<Session>();
            if (session != null)
            {
                session.LastHeartTime = MyTime.time;
            }

            //响应
            HeartBeatResponse resp = new HeartBeatResponse();
            conn.Send(resp);
        }
        private void _CheckHeatBeat(object state)
        {
            DateTime nowTime = DateTime.Now;
            //这里规定心跳包超过m_lastHeartbeatTimes秒没用更新就将连接清理
            foreach (var kv in m_lastHeartbeatTimes)
            {
                TimeSpan gap = nowTime - kv.Value;
                if (gap.TotalSeconds > m_heartBeatTimeOut)
                {
                    //关闭超时的客户端连接
                    Connection conn = kv.Key;
                    Log.Information("[心跳检查]心跳超时==>");//移除相关的资源
                    CloseConnection(conn);
                }
            }
        }
        private void _Send2OtherServerHeatBeatReq()
        {
            foreach(var v in m_curConnOtherServer.Values)
            {
                HeartBeatRequest req = new HeartBeatRequest();
                v.Send(req);
            }
        }
        private void _HeartBeatResponse(Connection sender, HeartBeatResponse message)
        {

        }
    }
}
