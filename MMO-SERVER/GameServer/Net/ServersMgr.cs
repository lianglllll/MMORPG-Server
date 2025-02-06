using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core;
using GameServer.Handle;
using GameServer.Hanle;
using GameServer.Utils;
using Google.Protobuf;
using Google.Protobuf.Collections;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;
using System.Collections.Generic;

namespace GameServer.Net
{
    class ServerEntry
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public NetClient NetClient { get; set; }
        public bool IsFirstConn { get; set; }
    }

    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode m_curSin;
        private Dictionary<SERVER_TYPE, ServerEntry> m_outgoingServerConnection = new();

        public void Init()
        {
            // 本服务器的信息
            m_curSin = new ServerInfoNode();
            GameServerInfoNode gNode = new GameServerInfoNode();
            m_curSin.ServerType = SERVER_TYPE.Game;
            m_curSin.Ip = Config.Server.ip;
            m_curSin.Port = Config.Server.serverPort;
            m_curSin.ServerId = 0;
            gNode.GameWorldId = Config.Server.gameWorldId;
            m_curSin.GameServerInfo = gNode;
            m_curSin.EventBitmap = SetEventBitmap();

            // 网络服务开启
            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                false, true, true,
                null, 0, null, null,
                Config.Server.ip, Config.Server.serverPort, null, ClusterServerDisconnected);
            GameMonitor.Instance.Init();
            GameServerHandler.Instance.Init();
            SessionManager.Instance.Init();
            GameTokenManager.Instance.Init();
            EnterGameWorldHanlder.Instance.Init(m_curSin.GameServerInfo.GameWorldId);

            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);

            _ExecutePhase0();
        }
        public void UnInit()
        {

        }
        private int SetEventBitmap()
        {
            int bitmap = 0;
            List<ClusterEventType> events = new List<ClusterEventType>
            {
                ClusterEventType.DbproxyEnter,
            };
            foreach (var e in events)
            {
                bitmap |= (1 << (int)e);
            }
            return bitmap;
        }

        // phase
        private bool _ExecutePhase0()
        {
            // 连接到控制中心cc
            _ConnectToCC();
            return true;
        }
        private bool _ExecutePhase1(RepeatedField<ClusterEventNode> clusterEventNodes)
        {
            foreach (var node in clusterEventNodes)
            {
                if (node.EventType == ClusterEventType.DbproxyEnter)
                {
                    AddDBServerInfo(node.ServerInfoNode);
                }
            }
            return true;
        }
        private bool _ExecutePhase2()
        {
            bool result = false;
            if (m_outgoingServerConnection.TryGetValue(SERVER_TYPE.Dbproxy, out var db) && db.NetClient != null )
            {
                _ExecutePhase3();
                result = true;
                goto End;
            }
        End:
            return result;
        }
        private bool _ExecutePhase3()
        {
            // 开始网络监听，预示着当前服务器的正式启动
            ConnManager.Instance.Start();
            Log.Information("\x1b[32m" + "Initialization complete, server is now operational." + "\x1b[0m");
            return true;
        }

        // net
        private void ClusterServerDisconnected(Connection conn)
        {
            int serverId = conn.Get<int>();
            if (serverId != 0)
            {
                GameMonitor.Instance.HaveInstanceDisconnection(serverId);
            }
        }

        // cc
        private void _ConnectToCC()
        {
            ConnManager.Instance.ConnctToServer(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the control center server.");

            var ccEntry = m_outgoingServerConnection.GetValueOrDefault(SERVER_TYPE.Controlcenter, null);
            if (ccEntry != null)
            {
                ccEntry.NetClient = tcpClient;
            }
            else
            {
                m_outgoingServerConnection.Add(SERVER_TYPE.Controlcenter, new ServerEntry { NetClient = tcpClient, IsFirstConn = true });
            }

            // 向cc注册自己
            ServerInfoRegisterRequest req = new();
            req.ServerInfoNode = m_curSin;
            tcpClient.Send(req);
        }
        private void _CCConnectedFailedCallback(NetClient tcpClient, bool isEnd)
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
        private void _CCDisconnectedCallback(NetClient tcpClient)
        {
            Log.Error("Disconnect from the ControlCenter server, attempting to reconnect controlCenter");
            var ccEntry = m_outgoingServerConnection.GetValueOrDefault(SERVER_TYPE.Controlcenter, null);
            ccEntry.NetClient = null;
            _ConnectToCC();
        }
        private void _RegisterServerInfo2ControlCenterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_curSin.ServerId = message.ServerId;
                Log.Information("Successfully registered to ControlCenter, Get serverId = [{0}]", message.ServerId);
                Log.Information("Get Subscription events: {0}", message.ClusterEventNodes);
                if (m_outgoingServerConnection[SERVER_TYPE.Controlcenter].IsFirstConn)
                {
                    _ExecutePhase1(message.ClusterEventNodes);
                    m_outgoingServerConnection[SERVER_TYPE.Controlcenter].IsFirstConn = false;
                }
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }

        // db
        public void AddDBServerInfo(ServerInfoNode sin)
        {
            if (!m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Dbproxy))
            {
                var entry = new ServerEntry();
                entry.ServerInfoNode = sin;
                m_outgoingServerConnection[SERVER_TYPE.Dbproxy] = entry;
                _ConnectToDB();
            }
        }
        private void _ConnectToDB()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Dbproxy].ServerInfoNode;
            ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _DBConnectedCallback, _DBConnectedFailedCallback, _DBDisconnectedCallback);
        }
        private void _DBConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the DBProxy server[{0}].", m_outgoingServerConnection[SERVER_TYPE.Dbproxy].ServerInfoNode.ServerId);
            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Dbproxy].NetClient = tcpClient;
            _ExecutePhase2();
        }
        private void _DBConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to DBProxy server failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to DBProxy server failed, attempting to reconnect DBProxy server");
                Log.Error("重连还没写");
            }

        }
        private void _DBDisconnectedCallback(NetClient tcpClient)
        {

        }

        // tools 
        public bool SendMsgToDBProxy(IMessage message)
        {
            m_outgoingServerConnection[SERVER_TYPE.Dbproxy].NetClient.Send(message);
            return true;
        }
    }
}