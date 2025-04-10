using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;
using GameGateServer.Utils;
using HS.Protobuf.GameGateMgr;
using GameGateServer.Handle;
using HS.Protobuf.Game;
using Google.Protobuf;
using HS.Protobuf.Scene;

namespace GameGateServer.Net
{
    class ServerEntry
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public NetClient NetClient { get; set; }
        public bool IsFirstConn { get; set; }
        public string Token { get; set; }
    }

    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_curSin;
        private Dictionary<SERVER_TYPE, ServerEntry> m_outgoingServerConnection = new();

        private Dictionary<int, ServerEntry> m_outgoing_SceneServerConnection = new();    // <sceneId, scene>
        private Dictionary<int, int> m_outgoing_SceneServerConnection2 = new();    // <serverId, sceneId>

        public string GameToken { get; private set; }

        public override void Init()
        {
            // 本服务器的信息
            m_curSin = new ServerInfoNode();
            GameGateServerInfoNode ggNode = new();
            m_curSin.ServerType = SERVER_TYPE.Gamegate;
            m_curSin.Ip = Config.Server.ip;
            m_curSin.Port = Config.Server.serverPort;
            m_curSin.ServerId = 0;
            m_curSin.GameGateServerInfo = ggNode;
            m_curSin.EventBitmap = SetEventBitmap();
            ggNode.UserPort = Config.Server.userPort;

            // 网络服务初始化
            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                true, false, true,
                Config.Server.ip, Config.Server.userPort, UserConnected, UserDisconnected,
                null, 0, null, null);
            SessionManager.Instance.Init();
            SecurityHandler.Instance.Init();
            GameGateServerHandler.Instance.Init();
            EnterGameWorldHanlder.Instance.Init();
            SceneHandler.Instance.Init();
            ChatHandler.Instance.Init();


            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Instance.Register<RegisterToGGMRequest>((int)GameGateMgrProtocl.RegisterToGgmReq);
            ProtoHelper.Instance.Register<RegisterToGGMResponse>((int)GameGateMgrProtocl.RegisterToGgmResp);
            ProtoHelper.Instance.Register<ExecuteGGCommandRequest>((int)GameGateMgrProtocl.ExecuteGgCommandReq);
            ProtoHelper.Instance.Register<ExecuteGGCommandResponse>((int)GameGateMgrProtocl.ExecuteGgCommandResp);
            ProtoHelper.Instance.Register<RegisterToGRequest>((int)GameProtocl.RegisterToGReq);
            ProtoHelper.Instance.Register<RegisterToGResponse>((int)GameProtocl.RegisterToGResp);
            ProtoHelper.Instance.Register<RegisterToSceneRequest>((int)SceneProtocl.RegisterToSceneReq);
            ProtoHelper.Instance.Register<RegisterToSceneResponse>((int)SceneProtocl.RegisterToSceneResp);
            ProtoHelper.Instance.Register<ExitGameRequest>((int)GameProtocl.ExitGameReq);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterToGGMResponse>(_HandleRegisterToGGMResponse);
            MessageRouter.Instance.Subscribe<ExecuteGGCommandRequest>(_ExecuteGGCommandRequest);
            MessageRouter.Instance.Subscribe<RegisterToGResponse>(_HandleRegisterToGResponse);
            MessageRouter.Instance.Subscribe<RegisterToSceneResponse>(_HandleRegisterToSceneResponse);

            // 开始流程
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
                ClusterEventType.GamegatemgrEnter,
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
        private bool _ExecutePhase1(Google.Protobuf.Collections.RepeatedField<ClusterEventNode> clusterEventNodes)
        {
            if (clusterEventNodes.Count == 0)
            {
                Log.Error("No GameGateMgr server information was obtained, the GameGateMgr server may not be start");
                return false;
            }

            foreach (var node in clusterEventNodes)
            {
                if (node.EventType == ClusterEventType.GamegatemgrEnter)
                {
                    // 目前集群只有一个
                    AddGGMServerInfo(node.ServerInfoNode);
                    break;
                }
            }

            return true;
        }
        private bool _ExecutePhase2()
        {
            // 开始网络监听，预示着当前服务器的正式启动
            ConnManager.Instance.Start();

            Log.Information("\x1b[32m" + "Initialization complete, server is now operational." + "\x1b[0m");
            return true;
        }
        private bool _ExecutePhase2_1()
        {
            ConnManager.Instance.UserStart();
            Log.Information("\x1b[32m" + "Initialization complete, server is now operational." + "\x1b[0m");
            return true;
        }

        // net
        private void UserConnected(Connection connection)
        {
        }
        private void UserDisconnected(Connection connection)
        {
            // session中移除他
            var session = connection.Get<Session>();
            if (session != null)
            {
                SessionManager.Instance.RemoveSessionById(session.Id);
                if (!string.IsNullOrEmpty(session.m_cId))
                {
                    // 通知game删除
                    var req = new ExitGameRequest();
                    req.GameToken = GameToken;
                    req.CharacterId = session.m_cId;
                    SendToGameServer(req);
                }
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

        // ggm
        public void AddGGMServerInfo(ServerInfoNode sin)
        {
            if (!m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Gamegatemgr))
            {
                var entry = new ServerEntry();
                entry.ServerInfoNode = sin;
                entry.IsFirstConn = true;
                m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr] = entry;
                _ConnectToGGM();
            }
        }
        private void _ConnectToGGM()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr].ServerInfoNode;
            ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _GameGateMgrConnectedCallback, _GameGateMgrConnectedFailedCallback, _GameGateMgrDisconnectedCallback);
        }
        private void _GameGateMgrConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the GameGateMgr server.");

            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr].NetClient = tcpClient;

            // 向ggm注册自己
            RegisterToGGMRequest req = new();
            req.ServerInfoNode = m_curSin;
            tcpClient.Send(req);
        }
        private void _GameGateMgrConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to GameGateMgr failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to GameGateMgr failed, attempting to reconnect GameGateMgr");
                Log.Error("重连还没写");
            }

        }
        private void _GameGateMgrDisconnectedCallback(NetClient tcpClient)
        {

        }
        private void _HandleRegisterToGGMResponse(Connection conn, RegisterToGGMResponse message)
        {
            if (message.ResultCode == 0)
            {
                // 注册成功我们等待分配任务。
                Log.Information("Successfully registered to GameGateMgr,waiting for the GameGateMgr server to assign tasks.");
                // 这里标志一次连接的成功
                if (m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr].IsFirstConn == true)
                {
                    m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr].IsFirstConn = false;
                }
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }

        // command
        private void _ExecuteGGCommandRequest(Connection conn, ExecuteGGCommandRequest message)
        {
            Log.Information("Recive GGM's Command = {0}", message);
            int resultCode = 0;
            switch (message.Command)
            {
                case GateCommand.Start:
                    _ExecuteStart(message);
                    break;
                case GateCommand.End:
                    _ExecuteEnd();
                    break;
                default:
                    Log.Error("[ServersMgr._ExecuteGGCommandRequest]未识别的命令");
                    break;
            }

            ExecuteGGCommandResponse resp = new();
            resp.ResultCode = resultCode;
            resp.ErrCommand = message.Command;
            conn.Send(resp);
        }
        private bool _ExecuteStart(ExecuteGGCommandRequest message)
        {
            bool result = false;
            if (message.GameServerInfoNode == null)
            {
                goto End;
            }

            if (!m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Game))
            {
                m_outgoingServerConnection[SERVER_TYPE.Game] = new ServerEntry
                {
                    ServerInfoNode = message.GameServerInfoNode,
                    IsFirstConn = true
                };
            }
            else
            {
                m_outgoingServerConnection[SERVER_TYPE.Game].ServerInfoNode = message.GameServerInfoNode;
            }
            _ConnectToG();
            result = true;
        End:
            return result;
        }
        private bool _ExecuteEnd()
        {
            // 1.移除game相关的连接
            if (m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Game) && m_outgoingServerConnection[SERVER_TYPE.Game].NetClient != null)
            {
                ConnManager.Instance.CloseOutgoingServerConnection(m_outgoingServerConnection[SERVER_TYPE.Game].NetClient);
            }
            GameToken = "";

            // 2.清空当前的user连接
            ConnManager.Instance.UserEnd();

            // 3.清空当前的scene连接
            foreach (var sEntry in m_outgoing_SceneServerConnection.Values)
            {
                ConnManager.Instance.CloseOutgoingServerConnection(sEntry.NetClient);
            }
            m_outgoing_SceneServerConnection.Clear();
            m_outgoing_SceneServerConnection2.Clear();

            return true;
        }

        // g
        private void _ConnectToG()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Game].ServerInfoNode;
            ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _GameConnectedCallback, _GameConnectedFailedCallback, _GameDisconnectedCallback);
        }
        private void _GameConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the Game server.");

            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Game].NetClient = tcpClient;

            //注册
            RegisterToGRequest req = new();
            req.ServerInfoNode = m_curSin;
            tcpClient.Send(req);
        }
        private void _GameConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to GameServer failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to GameServer failed, attempting to reconnect game");
                Log.Error("重连还没写");
            }

        }
        private void _GameDisconnectedCallback(NetClient tcpClient)
        {
            Log.Error("Disconnect from the Game server");
            m_outgoingServerConnection[SERVER_TYPE.Game].NetClient = null;
        }
        private void _HandleRegisterToGResponse(Connection conn, RegisterToGResponse message)
        {
            Log.Information("Successfully registered to the game server, {0}", message);

            GameToken = message.GameToken;
            if (m_outgoingServerConnection[SERVER_TYPE.Game].IsFirstConn == true)
            {
                _ExecutePhase2();
                m_outgoingServerConnection[SERVER_TYPE.Game].IsFirstConn = false;
            }
            else
            {
                _ExecutePhase2_1();
            }

            // 去连接scene
            foreach (var node in message.SceneInfoNodes)
            {
                AddSServerInfo(node);
            }
        }

        // SCENEs                                                                                                 
        public void AddSServerInfo(ServerInfoNode node)
        {
            Log.Information("A new scene regigster , {0}", node);
            if (!m_outgoing_SceneServerConnection.ContainsKey(node.SceneServerInfo.SceneId))
            {
                ServerEntry sEntry = new ServerEntry
                {
                    ServerInfoNode = node,
                    IsFirstConn = true
                };
                m_outgoing_SceneServerConnection.Add(node.SceneServerInfo.SceneId, sEntry);
                m_outgoing_SceneServerConnection2.Add(node.ServerId, node.SceneServerInfo.SceneId);
                sEntry.NetClient = _ConnectToS(node);
                sEntry.NetClient.ServerId = node.ServerId;
            }
        }
        private NetClient _ConnectToS(ServerInfoNode node)
        {
            return ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _SceneConnectedCallback, _SceneConnectedFailedCallback, _SceneDisconnectedCallback);
        }
        private void _SceneConnectedCallback(NetClient tcpClient)
        {
            int sceneId = m_outgoing_SceneServerConnection2.GetValueOrDefault(tcpClient.ServerId, 0);
            var entry = m_outgoing_SceneServerConnection.GetValueOrDefault(sceneId, null);
            if (entry != null)
            {
                Log.Information("Successfully connected to the Scene server,serverId = [{0}], sceneId = [{1}].",
                    entry.ServerInfoNode.ServerId, entry.ServerInfoNode.SceneServerInfo.SceneId);

                tcpClient.Connection.Set<int>(tcpClient.ServerId);

                // 注册
                RegisterToSceneRequest req = new();
                req.ServerInfoNode = m_curSin;
                tcpClient.Send(req);
            }
        }
        private void _SceneConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to SceneServer failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to SceneServer failed, attempting to reconnect secene");
                Log.Error("重连还没写");
            }

        }
        private void _SceneDisconnectedCallback(NetClient tcpClient)
        {
            Log.Error("Disconnect from the Scene server[{0}]", tcpClient.ServerId);
            int sceneId = m_outgoing_SceneServerConnection2.GetValueOrDefault(tcpClient.ServerId, 0);
            m_outgoing_SceneServerConnection.Remove(sceneId);
            m_outgoing_SceneServerConnection2.Remove(tcpClient.ServerId);
        }
        private void _HandleRegisterToSceneResponse(Connection conn, RegisterToSceneResponse message)
        {
            int serverId = conn.Get<int>();
            int sceneId = m_outgoing_SceneServerConnection2.GetValueOrDefault(serverId, 0);
            var entry = m_outgoing_SceneServerConnection.GetValueOrDefault(sceneId, null);
            if (entry != null)
            {
                Log.Information("Successfully registered to the scene server[{0}]", serverId);
            }
        }

        // tools
        public bool SendToGameServer(IMessage message)
        {
            return m_outgoingServerConnection[SERVER_TYPE.Game].NetClient.Send(message);
        }
        public bool SendToSceneServer(int sceneId, IMessage message)
        {
            var entry = m_outgoing_SceneServerConnection.GetValueOrDefault(sceneId, null);
            if (entry != null)
            {
                return entry.NetClient.Send(message);
            }
            return false;
        }

    }
}