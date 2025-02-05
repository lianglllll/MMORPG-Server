using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Game;
using HS.Protobuf.GameGateMgr;
using HS.Protobuf.Scene;
using SceneServer.Core.Scene;
using SceneServer.Handle;
using SceneServer.Utils;
using Serilog;

namespace SceneServer.Net
{
    class ServerEntry
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public NetClient NetClient { get; set; }
        public bool IsFirstConn { get; set; }
    }

    class GameGateEntry
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public Connection Connection { get; set; }    
    }


    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_curSin;
        private Dictionary<SERVER_TYPE, ServerEntry> m_outgoingServerConnection = new();
        private Dictionary<int, GameGateEntry> m_gameGateConn = new();

        public string GameToken { get; private set; }

        public void Init()
        {
            // 本服务器的信息
            m_curSin = new ServerInfoNode();
            SceneServerInfoNode sNode = new();
            m_curSin.ServerType = SERVER_TYPE.Scene;
            m_curSin.Ip = Config.Server.ip;
            m_curSin.Port = Config.Server.serverPort;
            m_curSin.ServerId = 0;
            m_curSin.SceneServerInfo = sNode;
            m_curSin.EventBitmap = SetEventBitmap();

            // 网络服务开启
            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                false, true, true,
                null, 0, null, null,
                Config.Server.ip, Config.Server.serverPort, null, ClusterServerDisconnected);
            SceneServerHandler.Instance.Init();
            EnterGameWorldHanlder.Instance.Init();

            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Instance.Register<RegisterToGGMRequest>((int)GameGateMgrProtocl.RegisterToGgmReq);
            ProtoHelper.Instance.Register<RegisterToGGMResponse>((int)GameGateMgrProtocl.RegisterToGgmResp);
            ProtoHelper.Instance.Register<ExecuteSCommandRequest>((int)GameGateMgrProtocl.ExecuteSCommandReq);
            ProtoHelper.Instance.Register<ExecuteSCommandResponse>((int)GameGateMgrProtocl.ExecuteSCommandResp);
            ProtoHelper.Instance.Register<RegisterToGRequest>((int)GameProtocl.RegisterToGReq);
            ProtoHelper.Instance.Register<RegisterToGResponse>((int)GameProtocl.RegisterToGResp);
            ProtoHelper.Instance.Register<RegisterToSceneRequest>((int)SceneProtocl.RegisterToSceneReq);
            ProtoHelper.Instance.Register<RegisterToSceneResponse>((int)SceneProtocl.RegisterToSceneResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterToGGMResponse>(_HandleRegisterToGGMResponse);
            MessageRouter.Instance.Subscribe<ExecuteSCommandRequest>(_HandleExecuteSCommandRequest);
            MessageRouter.Instance.Subscribe<RegisterToGResponse>(_HandleRegisterToGResponse);
            MessageRouter.Instance.Subscribe<RegisterToSceneRequest>(_HandleRegisterToSceneRequest);

            // 流程开始
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
            foreach (var node in clusterEventNodes)
            {
                if (node.EventType == ClusterEventType.DbproxyEnter)
                {
                    AddDBServerInfo(node.ServerInfoNode);
                }
                else if (node.EventType == ClusterEventType.GamegatemgrEnter)
                {
                    AddGGMServerInfo(node.ServerInfoNode);
                }
            }
            return true;
        }
        private bool _ExecutePhase2()
        {
            if(m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Dbproxy) 
                && m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Game))
            {
                _ExecutePhase3();
            }
            return true;
        }
        private bool _ExecutePhase3()
        {
            // 加载对应的场景资源
            SceneManager.Instance.Init(m_curSin.SceneServerInfo.SceneId);
            _ExecutePhase4();
            return true;
        }
        private bool _ExecutePhase4()
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
                HaveGameGateDisconnect(serverId);
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
                    m_outgoingServerConnection[SERVER_TYPE.Controlcenter].IsFirstConn = false;
                    _ExecutePhase1(message.ClusterEventNodes);
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
            Log.Information("Successfully connected to the GameGateMgr server[{0}].", 
                m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr].ServerInfoNode.ServerId);

            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Gamegatemgr].NetClient = tcpClient;

            // 向lgm注册自己
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
                Log.Information("Successfully registered this server information with the GameGateMgr.");
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
            Log.Information("Successfully connected to the DBProxy server[{0}].",
                m_outgoingServerConnection[SERVER_TYPE.Dbproxy].ServerInfoNode.ServerId);
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

        // command
        private void _HandleExecuteSCommandRequest(Connection conn, ExecuteSCommandRequest message)
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
                    Log.Error("[ServersMgr._HandleExecuteSCommandRequest]未识别的命令");
                    break;
            }

            ExecuteSCommandResponse resp = new();
            resp.ResultCode = resultCode;
            resp.ErrCommand = message.Command;
            conn.Send(resp);
        }
        private bool _ExecuteStart(ExecuteSCommandRequest message)
        {
            if (message.GameServerInfoNode != null)
            {
                m_outgoingServerConnection[SERVER_TYPE.Game] = new ServerEntry { ServerInfoNode = message.GameServerInfoNode };
                _ConnectToG();
                return true;
            }
            return false;
        }
        private bool _ExecuteEnd()
        {
            if (m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Game))
            {
                ConnManager.Instance.CloseOutgoingServerConnection(m_outgoingServerConnection[SERVER_TYPE.Game].NetClient);
                m_outgoingServerConnection.Remove(SERVER_TYPE.Game);
                return true;
            }
            return false;
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
            Log.Information("Successfully connected to the Game server[{0}].",
                m_outgoingServerConnection[SERVER_TYPE.Game].ServerInfoNode.ServerId);

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
                Log.Error("Connect to GameServer failed, attempting to reconnect GameServer");
                Log.Error("重连还没写");
            }

        }
        private void _GameDisconnectedCallback(NetClient tcpClient)
        {

        }
        private void _HandleRegisterToGResponse(Connection conn, RegisterToGResponse message)
        {
            Log.Information("Successfully registered to the game server, Get GameToken = {0}",
                message.GameToken);
            GameToken = message.GameToken;
            m_curSin.SceneServerInfo.SceneId = message.AllocateSceneId;
            _ExecutePhase2();
        }

        // gameGate
        private void _HandleRegisterToSceneRequest(Connection conn, RegisterToSceneRequest message)
        {
            Log.Information("GameGateInstance Register , {0}", message.ServerInfoNode);
            var entry = new GameGateEntry
            {
                ServerInfoNode = message.ServerInfoNode,
                Connection = conn,
            };
            m_gameGateConn.Add(message.ServerInfoNode.ServerId, entry);
            conn.Set<int>(message.ServerInfoNode.ServerId);
            RegisterToSceneResponse resp = new();
            resp.ResultCode = 0;
            conn.Send(resp);
        }
        public void HaveGameGateDisconnect(int gameGateServerId)
        {
            Log.Error("GameGateInstance Disconnection, serverId = {0}", gameGateServerId);
            m_gameGateConn.Remove(gameGateServerId);
        }   

        public Connection GetGameGateConnByServerId(int gameGateServerId)
        {
            m_gameGateConn.TryGetValue(gameGateServerId, out var entry);
            return entry.Connection;
        }
    }
}