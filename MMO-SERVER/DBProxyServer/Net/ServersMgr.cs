using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Utils;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;

namespace DBProxyServer.Net
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private bool isFirstStart;
        private ServerInfoNode? m_curServerInfoNode;
        public NetClient? ccClient;

        public int ServerId { get { return m_curServerInfoNode.ServerId; } }
        public void Init()
        {
            isFirstStart = true;

            // 本服务器的信息
            m_curServerInfoNode = new ServerInfoNode();
            DBProxyServerInfoNode dbpNode = new();
            m_curServerInfoNode.ServerType = SERVER_TYPE.Dbproxy;
            m_curServerInfoNode.Ip = Config.Server.ip;
            m_curServerInfoNode.Port = Config.Server.port;
            m_curServerInfoNode.ServerId = 0;
            m_curServerInfoNode.DbProxyServerInfo = dbpNode;
            m_curServerInfoNode.EventBitmap = 0;

            // 网络服务开启
            NetService.Instance.Init();

            // 协议注册
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            // 下面这个协议是重复注册过的，原因是时序问题会报错，我看得不舒服。
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);

            // 连接到控制中心cc
            _CCConnectToControlCenter();
        }
        public void UnInit()
        {

        }
        private bool _ExecutePhase1()
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
            ccClient = tcpClient;
            Log.Information("Successfully connected to the ControlCenter server.");
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
            _CCConnectToControlCenter();
        }
        private void _RegisterServerInfo2ControlCenterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_curServerInfoNode.ServerId = message.ServerId;
                Log.Information($"Successfully registered to ControlCenter, get serverId = [{message.ServerId}]");
                if(isFirstStart == true)
                {
                    isFirstStart = false;
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