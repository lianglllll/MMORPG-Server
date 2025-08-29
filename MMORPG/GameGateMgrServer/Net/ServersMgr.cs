using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateMgrServer.Core;
using GameGateMgrServer.Handle;
using GameGateMgrServer.Utils;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;

namespace GameGateMgrServer.Net
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_curServerInfoNode;
        public NetClient ccClient;
        private bool ccIsFirstConn;
        public int ServerId { get { return m_curServerInfoNode.ServerId; } }
        public void Init()
        {
            ccIsFirstConn = true;

            // 本服务器的信息
            m_curServerInfoNode = new ServerInfoNode();
            GameGateMgrServerInfoNode ggmNode = new();
            m_curServerInfoNode.ServerType = SERVER_TYPE.Gamegatemgr;
            m_curServerInfoNode.Ip = Config.Server.ip;
            m_curServerInfoNode.Port = Config.Server.port;
            m_curServerInfoNode.ServerId = 0;
            m_curServerInfoNode.GameGateMgrServerInfo = ggmNode;
            m_curServerInfoNode.EventBitmap = SetEventBitmap();

            // 网络服务开启
            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                false, true, true,
                null, 0, null, null,
                Config.Server.ip, Config.Server.port, null, ClusterServerDisconnected); 
            GGMMonitor.Instance.Init();
            GameGateMgrHandler.Instance.Init();

            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            // 下面这个协议是重复注册过的，原因是时序问题会报错，我看得不舒服。
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);

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
                ClusterEventType.GameEnter,
                ClusterEventType.GameExit,
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
            _ConnectToCC();
            return true;
        }
        private bool _ExecutePhase1(Google.Protobuf.Collections.RepeatedField<ClusterEventNode> clusterEventNodes)
        {
            GGMMonitor.Instance.AddGameServerInfos(clusterEventNodes);

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
                GGMMonitor.Instance.EntryDisconnection(serverId);
            }
        }

        // cc
        private void _ConnectToCC()
        {
            ConnManager.Instance.ConnctToServer(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            ccClient = tcpClient;
            Log.Information("Successfully connect to the ControlCenter server.");
            //向cc注册自己
            ServerInfoRegisterRequest req = new();
            req.ServerInfoNode = m_curServerInfoNode;
            ccClient.Send(req);
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
            ccClient = null;
            _ConnectToCC();
        }
        private void _RegisterServerInfo2ControlCenterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_curServerInfoNode.ServerId = message.ServerId;
                Log.Information("Successfully registered to ControlCenter, Get serverId = [{0}]", message.ServerId);
                Log.Information("Get Subscription events: {0}", message.ClusterEventNodes);
                if (ccIsFirstConn == true)
                {
                    ccIsFirstConn = false;
                    _ExecutePhase1(message.ClusterEventNodes);
                }
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
    }
}