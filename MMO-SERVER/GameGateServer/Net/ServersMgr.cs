using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGateMgr;
using Serilog;
using GameGateServer.Utils;
using HS.Protobuf.GameGateMgr;
using GameGateServer.Handle;
using HS.Protobuf.Login;
using HS.Protobuf.Game;

namespace GameGateServer.Net
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
        public string GameToken { get; private set; }

        public void Init()
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
            NetService.Instance.Init();
            SessionManager.Instance.Init();
            SecurityHandler.Instance.Init();
            GameGateHandler.Instance.Init();
            EnterGameWorldHanlder.Instance.Init();

            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Instance.Register<RegisterToGGMRequest>((int)GameGateMgrProtocl.RegisterToGgmReq);
            ProtoHelper.Instance.Register<RegisterToGGMResponse>((int)GameGateMgrProtocl.RegisterToGgmResp);
            ProtoHelper.Instance.Register<ExecuteGGCommandRequest>((int)GameGateMgrProtocl.ExecuteGgCommandReq);
            ProtoHelper.Instance.Register<ExecuteGGCommandResponse>((int)GameGateMgrProtocl.ExecuteGgCommandResp);
            ProtoHelper.Instance.Register<GetGameTokenRequest>((int)GameProtocl.GetGameTokenReq);
            ProtoHelper.Instance.Register<GetGameTokenResponse>((int)GameProtocl.GetGameTokenResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterToGGMResponse>(_HandleRegisterToGGMResponse);
            MessageRouter.Instance.Subscribe<ExecuteGGCommandRequest>(_ExecuteGGCommandRequest);
            MessageRouter.Instance.Subscribe<GetGameTokenResponse>(_HandleGetGameTokenResponse);

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
                Log.Error("No GameGateMgr server information was obtained.");
                Log.Error("The GameGateMgr server may not be start.");
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
            Log.Information("waiting for the GameGateMgr server to assign tasks.");
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
            m_outgoingServerConnection.Add(SERVER_TYPE.Controlcenter, new ServerEntry { NetClient = tcpClient });

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
            NetService.Instance.ConnctToServer(node.Ip, node.Port,
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
                Log.Information("Successfully registered this server information with the GameGateMgr.");
                _ExecutePhase2();
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }

        // command
        private void _ExecuteGGCommandRequest(Connection conn, ExecuteGGCommandRequest message)
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
            if(message.GameServerInfoNode != null)
            {
                Log.Information("Command:Start......");
                m_outgoingServerConnection[SERVER_TYPE.Game] = new ServerEntry { ServerInfoNode = message.GameServerInfoNode };
                _ConnectToG();
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
            if(m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Game))
            {
                NetService.Instance.CloseServerConnection(m_outgoingServerConnection[SERVER_TYPE.Game].NetClient);
                m_outgoingServerConnection.Remove(SERVER_TYPE.Game);
                return true;
            }
            return false;
        }

        // g
        private void _ConnectToG()
        {
            ServerInfoNode node = m_outgoingServerConnection[SERVER_TYPE.Game].ServerInfoNode;
            NetService.Instance.ConnctToServer(node.Ip, node.Port,
                _GameConnectedCallback, _GameConnectedFailedCallback, _GameDisconnectedCallback);
        }
        private void _GameConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the Game server.");

            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Game].NetClient = tcpClient;
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
                Log.Error("Connect to GameServer failed, attempting to reconnect login");
                Log.Error("重连还没写");
            }

        }
        private void _GameDisconnectedCallback(NetClient tcpClient)
        {

        }
        private void _HandleGetGameTokenResponse(Connection sender, GetGameTokenResponse message)
        {
            GameToken = message.GameToken;
            _ExecutePhase3();
        }

    }
}