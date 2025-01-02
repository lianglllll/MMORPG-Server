using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.GameGateMgr;
using HS.Protobuf.LoginGateMgr;
using SceneServer.Core;
using SceneServer.Utils;
using Serilog;

namespace SceneServer.Net
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
            NetService.Instance.Init();
            SceneHandler.Instance.Init();

            // 协议注册
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Register<RegisterToGGMRequest>((int)GameGateMgrProtocl.RegisterToGgmReq);
            ProtoHelper.Register<RegisterToGGMResponse>((int)GameGateMgrProtocl.RegisterToGgmResp);
            ProtoHelper.Register<ExecuteSCommandRequest>((int)GameGateMgrProtocl.ExecuteSCommandReq);
            ProtoHelper.Register<ExecuteSCommandResponse>((int)GameGateMgrProtocl.ExecuteSCommandResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterToGGMResponse>(_HandleRegisterToGGMResponse);
            MessageRouter.Instance.Subscribe<ExecuteSCommandRequest>(_HandleExecuteSCommandRequest);

            // 流程开始
            _ExecutePhase1();
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


        private bool _ExecutePhase1()
        {
            // 连接到控制中心cc
            var cEntry = new ServerEntry();
            m_outgoingServerConnection.Add(SERVER_TYPE.Controlcenter, cEntry);
            _CCConnectToControlCenter();
            return true;
        }
        private bool _ExecutePhase2(Google.Protobuf.Collections.RepeatedField<ClusterEventNode> clusterEventNodes)
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
        private bool _ExecutePhase3()
        {
            if(m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Dbproxy) && m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Gamegatemgr)
                && m_outgoingServerConnection.ContainsKey(SERVER_TYPE.Game))
            {
                _ExecutePhase4();
            }
            return true;
        }
        private bool _ExecutePhase4()
        {
            // 开始网络监听，预示着当前服务器的正式启动
            NetService.Instance.Start();
            return true;
        }

        // cc
        private void _CCConnectToControlCenter()
        {
            NetService.Instance.ConnctToServer(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            m_outgoingServerConnection[SERVER_TYPE.Controlcenter].NetClient = tcpClient;
            Log.Information("[Successfully connected to the control center server.]");

            //向cc注册自己
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
            
        }
        private void _RegisterServerInfo2ControlCenterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_curSin.ServerId = message.ServerId;
                Log.Information("[Successfully registered this server information with the ControlCenter.]");
                Log.Information($"The server ID of this server is [{message.ServerId}]");
                _ExecutePhase2(message.ClusterEventNodes);
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
                _ExecutePhase3();
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
            NetService.Instance.ConnctToServer(node.Ip, node.Port,
                _DBConnectedCallback, _DBConnectedFailedCallback, _DBDisconnectedCallback);
        }
        private void _DBConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the DBProxy server.");
            // 记录
            m_outgoingServerConnection[SERVER_TYPE.Dbproxy].NetClient = tcpClient;
            _ExecutePhase3();
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

        // g
        private void _HandleExecuteSCommandRequest(Connection sender, ExecuteSCommandRequest message)
        {
            var entry = new ServerEntry();
            entry.ServerInfoNode = message.GameServerInfoNode;
            m_outgoingServerConnection[SERVER_TYPE.Game] = entry;
            _ConnectToG();

            ExecuteGGCommandResponse resp = new();
            resp.ResultCode = 0;
            sender.Send(resp);
        }
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

            _ExecutePhase3();
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
    }
}