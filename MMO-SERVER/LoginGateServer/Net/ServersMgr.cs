using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Login;
using HS.Protobuf.LoginGateMgr;
using LoginGateServer.Core;
using LoginGateServer.Handle;
using LoginGateServer.Utils;
using Serilog;
using System.Net.Sockets;

namespace LoginGateServer.Net
{
    class ServerEntry
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public NetClient NetClient { get; set; }
    }

    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_curSin;
        private Dictionary<SERVER_TYPE, ServerEntry> m_outgoingServerConnection = new();
        public string LoginToken { get; private set; }
        public void Init()
        {
            // 网络服务初始化
            NetService.Instance.Init();
            LoginGateHandler.Instance.Init();
            UserHandler.Instance.Init();
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
            ProtoHelper.Instance.Register<GetLoginTokenRequest>((int)LoginProtocl.GetLoginTokenReq);
            ProtoHelper.Instance.Register<GetLoginTokenResponse>((int)LoginProtocl.GetLoginTokenResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterLoginGateInstanceResponse>(_RegisterLoginGateInstanceResponse);
            MessageRouter.Instance.Subscribe<ExecuteLGCommandRequest>(_ExecuteLGCommandRequest);
            MessageRouter.Instance.Subscribe<GetLoginTokenResponse>(_HandleGetLoginTokenResponse);

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
            Log.Information("waiting for the LoginGateMgr server to assign tasks.");
            return true;
        }
        private bool _ExecutePhase3()
        {
            // 开始网络监听，预示着当前服务器的正式启动
            NetService.Instance.Start();
            
            return true;
        }

        // cc
        private void _ConnectToCC()
        {
            NetService.Instance.ConnctToServer(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the control center server.");
            // 记录
            m_outgoingServerConnection.Add(SERVER_TYPE.Controlcenter, new ServerEntry { NetClient = tcpClient});

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
            
        }
        private void _RegisterServerInfo2ControlCenterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_curSin.ServerId = message.ServerId;
                Log.Information("Successfully registered this server information with the ControlCenter.");
                Log.Information($"The server ID of this server is [{message.ServerId}]");
                _ExecutePhase1(message.ClusterEventNodes);
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
            NetService.Instance.ConnctToServer(node.Ip, node.Port,
                _LoginGateMgrConnectedCallback, _LoginGateMgrConnectedFailedCallback, _LoginGateMgrDisconnectedCallback);
        }
        private void _LoginGateMgrConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the LoginGateMgr server.");

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
                Log.Error("重连还没写");
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
                Log.Information("Successfully registered this server information with the LoginGateMgr.");
                _ExecutePhase2();
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
        
        // command
        private void _ExecuteLGCommandRequest(Connection conn, ExecuteLGCommandRequest message)
        {
            int resultCode = 0;
            switch (message.Command)
            {
                case GateCommand.Start:
                    _ExecuteStart(message);
                    break;
                case GateCommand.Stop:
                    _ExecuteStop();
                    break;
                case GateCommand.Resume:
                    _ExecuteResume();
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
                Log.Information("Command:Start......");
                var entry = new ServerEntry();
                entry.ServerInfoNode = message.LoginServerInfoNode;
                m_outgoingServerConnection[SERVER_TYPE.Login] = entry;
                _ConnectToL();
                return true;
            }
            return false;
        }
        private bool _ExecuteStop()
        {
            // 停止当前的accept即可
            NetService.Instance.Stop();
            return true;
        }
        private bool _ExecuteResume()
        {
            NetService.Instance.Resume();
            return true;
        }
        private bool _ExecuteEnd()
        {
            if (m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Login))
            {
                NetService.Instance.CloseServerConnection(m_outgoingServerConnection[SERVER_TYPE.Login].NetClient);
                m_outgoingServerConnection.Remove(SERVER_TYPE.Login);
                return true;
            }
            return false;
        }

        // l
        private void _ConnectToL()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Login].ServerInfoNode;
            NetService.Instance.ConnctToServer(node.Ip, node.Port,
                _LoginConnectedCallback, _LoginConnectedFailedCallback, _LoginDisconnectedCallback);
        }
        private void _LoginConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the Login server.");
            m_outgoingServerConnection[SERVER_TYPE.Login].NetClient = tcpClient;
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

        }
        private void _HandleGetLoginTokenResponse(Connection sender, GetLoginTokenResponse message)
        {
            LoginToken = message.LoginToken;
            _ExecutePhase3();
        }


        // tools
        public void SentToLoginServer(IMessage message)
        {
            m_outgoingServerConnection[SERVER_TYPE.Login].NetClient.Send(message);
        }
    }
}