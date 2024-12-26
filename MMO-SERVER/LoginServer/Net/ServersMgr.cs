using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using LoginServer.Utils;
using Serilog;

namespace LoginServer.Net
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode m_curServerInfoNode;
        private Dictionary<SERVER_TYPE, NetClient> m_outgoingServerConnection = new();

        public void Init()
        {
            // 本服务器的信息
            m_curServerInfoNode = new ServerInfoNode();
            LoginServerInfoNode lNode = new LoginServerInfoNode();
            m_curServerInfoNode.ServerType = SERVER_TYPE.Login;
            m_curServerInfoNode.Ip = Config.Server.ip;
            m_curServerInfoNode.Port = Config.Server.port;
            m_curServerInfoNode.ServerId = 0;
            m_curServerInfoNode.LoginServerInfo = lNode;

            // 网络服务开启
            NetService.Instance.Init();

            // 协议注册
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);

            // 连接到控制中心cc
            _CCConnectToControlCenter();

        }
        public void UnInit()
        {

        }

        // cc
        private void _CCConnectToControlCenter()
        {
            NetService.Instance.ConnctToServer(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            m_outgoingServerConnection.Add(SERVER_TYPE.Controlcenter, tcpClient);
            Log.Information("[Successfully connected to the control center server.]");
            //向cc注册自己
            ServerInfoRegisterRequest req = new();
            req.ServerInfoNode = m_curServerInfoNode;
            m_outgoingServerConnection[SERVER_TYPE.Controlcenter]?.Send(req);
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
                m_curServerInfoNode.ServerId = message.ServerId;
                Log.Information("[Successfully registered this server information with the ControlCenter.]");
                Log.Information($"The server ID of this server is [{message.ServerId}]");
                // 开始网络监听，预示着当前服务器的正式启动
                NetService.Instance.Init2();
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
    }
}