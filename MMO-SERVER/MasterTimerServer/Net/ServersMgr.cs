using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using MasterTimerServer.Utils;
using Serilog;

namespace MasterTimerServer.Core
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_sin;

        private bool isFirstConnectToCC;
        public NetClient? ccClient;

        public bool Init()
        {
            isFirstConnectToCC = true;

            // 本服务器的信息
            m_sin = new ServerInfoNode();
            MasterTimeServerInfoNode mtNode = new();
            m_sin.ServerType = SERVER_TYPE.Mastertime;
            m_sin.Ip = Config.Server.ip;
            m_sin.Port = Config.Server.serverPort;
            m_sin.ServerId = 0;
            m_sin.MasterTimeServerInfo = mtNode;
            m_sin.EventBitmap = 0;

            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                false, true, false,
                null, 0, null, null,
                Config.Server.ip, Config.Server.serverPort, ClusterServerConnected, ClusterServerDisconnected);
            PrecisionTimeService.Instance.Init();

            // proto注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            // 消息订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_HandleServerInfoRegisterResponse);

            _ExecutePhase0();
            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        private void ClusterServerConnected(Connection conn)
        {
            Log.Information("A slaveTimeServer connected to masterTimeServer.");
        }
        private void ClusterServerDisconnected(Connection conn)
        {
            Log.Information("A slaveTimeServer disconnected from masterTimeServer.");
        }

        private bool _ExecutePhase0()
        {
            _ConnectToCC();
            return true;
        }
        private bool _ExecutePhase1()
        {
            // 开始网络监听，预示着当前服务器的正式启动
            ConnManager.Instance.Start();
            Log.Information("\x1b[32m" + "Initialization complete, server is now operational." + "\x1b[0m");
            return true;
        }

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
            req.ServerInfoNode = m_sin;
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
        private void _HandleServerInfoRegisterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_sin.ServerId = message.ServerId;
                Log.Information("Successfully registered to ControlCenter, Get serverId = [{0}]", message.ServerId);
                Log.Information("Get Subscription events: {0}", message.ClusterEventNodes);
                if (isFirstConnectToCC == true)
                {
                    isFirstConnectToCC = false;
                    _ExecutePhase1();
                }
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
    }
}
