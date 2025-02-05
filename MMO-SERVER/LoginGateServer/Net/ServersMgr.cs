using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Game;
using HS.Protobuf.Login;
using HS.Protobuf.LoginGateMgr;
using LoginGateServer.Handle;
using LoginGateServer.Utils;
using Serilog;

namespace LoginGateServer.Net
{
    class ServerEntry
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public NetClient NetClient { get; set; }
        public bool IsFirstConn { get; set; }
    }

    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_curSin;
        private Dictionary<SERVER_TYPE, ServerEntry> m_outgoingServerConnection = new();
        public string LoginToken { get; private set; }
        public void Init()
        {
            // 网络服务初始化
            ConnManager.Instance.Init(Config.Server.workerCount, 0, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                true, false, true,
                Config.Server.ip, Config.Server.userPort, UserConnected, UserDisconnected,
                null, 0, null, null); LoginGateHandler.Instance.Init();
            UserHandler.Instance.Init();
            EnterGameWorldHanlder.Instance.Init();
            LoginGateTokenManager.Instance.Init();
            SecurityHandler.Instance.Init();

            // 本服务器的信息
            m_curSin = new ServerInfoNode();
            LoginGateServerInfoNode lgNode = new();
            m_curSin.ServerType = SERVER_TYPE.Logingate;
            m_curSin.Ip = Config.Server.ip;
            m_curSin.Port = Config.Server.serverPort;
            m_curSin.ServerId = 0;
            m_curSin.LoginGateServerInfo = lgNode;
            m_curSin.EventBitmap = SetEventBitmap();

            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Instance.Register<RegisterLoginGateInstanceRequest>((int)LoginGateMgrProtocl.RegisterLogingateInstanceReq);
            ProtoHelper.Instance.Register<RegisterLoginGateInstanceResponse>((int)LoginGateMgrProtocl.RegisterLogingateInstanceResp);
            ProtoHelper.Instance.Register<ExecuteLGCommandRequest>((int)LoginGateMgrProtocl.ExecuteLgCommandReq);
            ProtoHelper.Instance.Register<ExecuteLGCommandResponse>((int)LoginGateMgrProtocl.ExecuteLgCommandResp);
            ProtoHelper.Instance.Register<RegisterToLRequest>((int)LoginProtocl.RegisterToLReq);
            ProtoHelper.Instance.Register<RegisterToLResponse>((int)LoginProtocl.RegisterToLResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterLoginGateInstanceResponse>(_RegisterLoginGateInstanceResponse);
            MessageRouter.Instance.Subscribe<ExecuteLGCommandRequest>(_ExecuteLGCommandRequest);
            MessageRouter.Instance.Subscribe<RegisterToLResponse>(_HandleRegisterToLResponse);

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
                ClusterEventType.LogingatemgrEnter,
            };
            foreach(var e in events)
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
                Log.Error("No LoginGateMgr server information was obtained.");
                Log.Error("The LoginGateMgr server may not be start.");
                return false;
            }

            foreach (var node in clusterEventNodes)
            {
                if (node.EventType == ClusterEventType.LogingatemgrEnter)
                {
                    AddLGMServerInfo(node.ServerInfoNode);
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

        // net
        private void UserConnected(Connection conn)
        {
            // 分配一下连接token
            LoginGateToken token = LoginGateTokenManager.Instance.NewToken(conn);
            conn.Set<LoginGateToken>(token);
        }
        private void UserDisconnected(Connection conn)
        {
            // token回收
            LoginGateTokenManager.Instance.RemoveToken(conn.Get<LoginGateToken>().Id);
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
        private void _CCConnectedFailedCallback(NetClient tcpClient,bool isEnd)
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

        // lgm
        public void AddLGMServerInfo(ServerInfoNode sin)
        {
            if (!m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Logingatemgr))
            {
                var entry = new ServerEntry();
                entry.ServerInfoNode = sin;
                m_outgoingServerConnection[SERVER_TYPE.Logingatemgr] = entry;
                _ConnectToLGM();
            }
        }
        private void _ConnectToLGM()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Logingatemgr].ServerInfoNode;
            ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _LoginGateMgrConnectedCallback, _LoginGateMgrConnectedFailedCallback, _LoginGateMgrDisconnectedCallback);
        }
        private void _LoginGateMgrConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the LoginGateMgr server[{0}].", m_outgoingServerConnection[SERVER_TYPE.Logingatemgr].ServerInfoNode.ServerId);

            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Logingatemgr].NetClient = tcpClient;

            // 向lgm注册自己
            RegisterLoginGateInstanceRequest req = new();
            req.ServerInfoNode = m_curSin;
            tcpClient.Send(req);
        }
        private void _LoginGateMgrConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to loginGateMgr failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to loginGateMgr failed, attempting to reconnect loginGateMgr");
            }

        }
        private void _LoginGateMgrDisconnectedCallback(NetClient tcpClient)
        {

        }
        private void _RegisterLoginGateInstanceResponse(Connection conn, RegisterLoginGateInstanceResponse message)
        {
            if (message.ResultCode == 0)
            {
                // 注册成功我们等待分配任务。
                Log.Information("Successfully registered this server information with the LoginGateMgr, waiting LoginGateMgr's task");
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
        
        // command
        private void _ExecuteLGCommandRequest(Connection conn, ExecuteLGCommandRequest message)
        {
            Log.Information("Recive LGM's Command = {0}", message);
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
                    Log.Error("[ServersMgr._ExecuteLGCommandRequest]未识别的命令");
                    break;
            }

            ExecuteLGCommandResponse resp = new();
            resp.ResultCode = resultCode;
            resp.ErrCommand = message.Command;
            conn.Send(resp);
        }
        private bool _ExecuteStart(ExecuteLGCommandRequest message)
        {
            if (message.LoginServerInfoNode != null)
            {
                var entry = new ServerEntry();
                entry.ServerInfoNode = message.LoginServerInfoNode;
                m_outgoingServerConnection[SERVER_TYPE.Login] = entry;
                _ConnectToL();
                return true;
            }
            return false;
        }
        private bool _ExecuteEnd()
        {
            // 断开和l的连接
            if (m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Login))
            {
                ConnManager.Instance.CloseOutgoingServerConnection(m_outgoingServerConnection[SERVER_TYPE.Login].NetClient);
                m_outgoingServerConnection.Remove(SERVER_TYPE.Login);
            }
            LoginToken = "";

            ConnManager.Instance.UserEnd();
            return true;
        }

        // l
        private void _ConnectToL()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Login].ServerInfoNode;
            ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _LoginConnectedCallback, _LoginConnectedFailedCallback, _LoginDisconnectedCallback);
        }
        private void _LoginConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the Login server[{0}].", m_outgoingServerConnection[SERVER_TYPE.Login].ServerInfoNode.ServerId);
            m_outgoingServerConnection[SERVER_TYPE.Login].NetClient = tcpClient;

            //注册
            RegisterToLRequest req = new();
            req.ServerInfoNode = m_curSin;
            tcpClient.Send(req);
        }
        private void _LoginConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to login failed, the server may not be turned on");
                
                //做一下重新连接或者其他
                Log.Error("重连还没写");

            }
            else
            {
                Log.Error("Connect to login failed, attempting to reconnect login");
            }

        }
        private void _LoginDisconnectedCallback(NetClient tcpClient)
        {
            // 暂时 目前lg只连接一个l
            Log.Error("Disconnect from the Login server[{0}]", m_outgoingServerConnection[SERVER_TYPE.Login].ServerInfoNode.ServerId);
            // 暂时 清理当前的l信息
            m_outgoingServerConnection.Remove(SERVER_TYPE.Login);
            // lgm是否能感知到？

        }
        private void _HandleRegisterToLResponse(Connection sender, RegisterToLResponse message)
        {
            Log.Information("Successfully registered to the login server.");
            LoginToken = message.LoginToken;
            _ExecutePhase2();
        }

        // tools
        public bool SendToLoginServer(IMessage message)
        {
            bool result = false;
            if (!m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Login))
            {
                result = false;
                goto End;
            }
            m_outgoingServerConnection[SERVER_TYPE.Login].NetClient.Send(message);
            result = true;
        End:
            return result;
        }
    }
}