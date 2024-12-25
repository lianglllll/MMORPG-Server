using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using GameServer.Utils;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;
using System.Collections.Generic;

namespace GameServer.Net
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode m_curServerInfoNode;
        private Dictionary<SERVER_TYPE, TcpClient> m_outgoingServerConnection = new();

        public void Init()
        {
            // 本服务器的信息
            m_curServerInfoNode = new ServerInfoNode();
            GameServerInfoNode gNode = new GameServerInfoNode();
            m_curServerInfoNode.ServerType = SERVER_TYPE.Game;
            m_curServerInfoNode.Ip = Config.Server.ip;
            m_curServerInfoNode.Port = Config.Server.userPort;
            m_curServerInfoNode.ServerId = 0;
            gNode.GameWorldId = Config.Server.gameWorldId;
            m_curServerInfoNode.GameServerInfo = gNode;

            // 网络服务开启
            NetService.Instance.Init();

            // 协议注册
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
            ProtoHelper.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<SSHeartBeatResponse>(_SSHeartBeatResponse);

            // 连接到控制中心cc
            TcpClient ccClient = new TcpClient();
            ccClient.Init(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
            m_outgoingServerConnection.Add(SERVER_TYPE.Controlcenter, ccClient);

            // 定时发送ss心跳包
            Scheduler.Instance.AddTask(_SendSSHeatBeatReq, Config.Server.heartBeatSendInterval, 0);
        }
        public void UnInit()
        {

        }

        private void _CCConnectedCallback(Connection conn)
        {
            Log.Information("Successfully connected to the control center server.");
            //向cc注册自己
            ServerInfoRegisterRequest req = new();
            ServerInfoNode node = new();
            GameServerInfoNode gNode = new();
            node.ServerType = SERVER_TYPE.Game;
            node.Ip = Config.Server.ip;
            node.Port = Config.Server.userPort;
            gNode.GameWorldId = Config.Server.gameWorldId;
            node.GameServerInfo = gNode;
            req.ServerInfoNode = node;
            m_outgoingServerConnection[SERVER_TYPE.Controlcenter]?.Send(req);
        }
        private void _CCConnectedFailedCallback(bool isEnd)
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
        private void _CCDisconnectedCallback(Connection conn)
        {
            
        }
        private void _RegisterServerInfo2ControlCenterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_curServerInfoNode.ServerId = message.ServerId;
                Log.Information("Successfully registered this server information with the ControlCenter.");
                Log.Information($"The server ID of this server is [{message.ServerId}]");
                // 开始网络监听，预示着当前服务器的正式启动
                NetService.Instance.Init2();
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }

        private void _SendSSHeatBeatReq()
        {
            foreach (var v in m_outgoingServerConnection.Values)
            {
                SSHeartBeatRequest req = new SSHeartBeatRequest();
                v.Send(req);
            }
        }
        private void _SSHeartBeatResponse(Connection sender, SSHeartBeatResponse message)
        {
            // 知道对端也活着，嘻嘻。
        }
    }
}

