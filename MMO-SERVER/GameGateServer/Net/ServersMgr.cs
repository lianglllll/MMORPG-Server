using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGateMgr;
using GameGateServer.Core;
using Serilog;
using GameGateServer.Utils;
using HS.Protobuf.GameGateMgr;

namespace GameGateServer.Net
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private ServerInfoNode? m_curSin;
        private ServerInfoNode? m_curGGMSin;
        private ServerInfoNode? m_curGSin;
        private NetClient? m_CCClient;
        private NetClient? m_GGMClient;
        private NetClient? m_GClient;
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

            // 网络服务初始化
            NetService.Instance.Init();

            // 协议注册
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
            ProtoHelper.Register<RegisterGameGateInstanceRequest>((int)GameGateMgrProtocl.RegisterGgInstanceReq);
            ProtoHelper.Register<RegisterGameGateInstanceResponse>((int)GameGateMgrProtocl.RegisterGgInstanceResp);
            ProtoHelper.Register<GetAllServerInfoRequest>((int)ControlCenterProtocl.GetAllserverinfoReq);
            ProtoHelper.Register<GetAllServerInfoResponse>((int)ControlCenterProtocl.GetAllserverinfoResp);
            ProtoHelper.Register<ExecuteGGCommandRequest>((int)GameGateMgrProtocl.ExecuteGgCommandReq);
            ProtoHelper.Register<ExecuteGGCommandResponse>((int)GameGateMgrProtocl.ExecuteGgCommandResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_RegisterServerInfo2ControlCenterResponse);
            MessageRouter.Instance.Subscribe<RegisterGameGateInstanceResponse>(_HandleRegisterGameGateInstanceResponse);
            MessageRouter.Instance.Subscribe<GetAllServerInfoResponse>(_HandleGetAllServerInfoResponse);
            MessageRouter.Instance.Subscribe<ExecuteGGCommandRequest>(_ExecuteGGCommandRequest);

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

        // 零阶段：连接cc
        // 一阶段：cc注册完成,获取gatemgr信息
        // 二阶段：lgm注册完成，等待分配任务
        // 三阶段：l连接成功，等待接受用户连接
        private bool _ExecutePhase0()
        {
            // 连接到控制中心cc
            _ConnectToCC();
            return true;
        }
        private bool _ExecutePhase1()
        {
            // 获取LoginGateMgr的信息
            var req = new GetAllServerInfoRequest();
            req.ServerType = SERVER_TYPE.Gamegatemgr;
            m_CCClient.Send(req);
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
            ClientMessageDispatcher.Instance.Init();
            return true;
        }

        private void _ConnectToCC()
        {
            NetService.Instance.ConnctToServer(Config.CCConfig.ip, Config.CCConfig.port, _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the control center server.");
            // 记录
            m_CCClient = tcpClient;

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
                _ExecutePhase1();
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
       
        private void _HandleGetAllServerInfoResponse(Connection conn, GetAllServerInfoResponse message)
        {
            var list = message.ServerInfoNodes;
            if (list.Count == 0)
            {
                Log.Error("No GameGateMgr server information was obtained.");
                Log.Error("The GameGateMgr server may not be start.");
                return;
            }
            if (m_curGGMSin == null)
            {
                m_curGGMSin = list[0];    // 集群只有一个
                _ConnectToGGM();
            }
        }
        public void SetGGMAndConnect(ServerInfoNode sin)
        {
            if(m_curGGMSin == null)
            {
                m_curGGMSin = sin;
                _ConnectToGGM();
            }
        }
        private void _ConnectToGGM()
        {
            NetService.Instance.ConnctToServer(m_curGGMSin.Ip, m_curGGMSin.Port,
                _GameGateMgrConnectedCallback, _GameGateMgrConnectedFailedCallback, _GameGateMgrDisconnectedCallback);
        }
        private void _GameGateMgrConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the GameGateMgr server.");
            
            // 记录
            m_GGMClient = tcpClient;

            // 向lgm注册自己
            RegisterLoginGateInstanceRequest req = new();
            req.ServerInfoNode = m_curSin;
            m_GGMClient.Send(req);
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
        private void _HandleRegisterGameGateInstanceResponse(Connection conn, RegisterGameGateInstanceResponse message)
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

        private void _ConnectToG()
        {
            NetService.Instance.ConnctToServer(m_curGSin.Ip, m_curGSin.Port,
                _GameConnectedCallback, _GameConnectedFailedCallback, _GameDisconnectedCallback);
        }
        private void _GameConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the Game server.");

            // 记录
            m_GClient = tcpClient;

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
                Log.Error("Connect to GameServer failed, attempting to reconnect login");
                Log.Error("重连还没写");
            }

        }
        private void _GameDisconnectedCallback(NetClient tcpClient)
        {

        }

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

            ExecuteLGCommandResponse resp = new();
            resp.ResultCode = resultCode;
            resp.ErrCommand = message.Command;
            conn.Send(resp);
        }
        private bool _ExecuteStart(ExecuteGGCommandRequest message)
        {
            if(message.GameServerInfoNode != null)
            {
                Log.Information("Command:Start......");
                m_curGSin = message.GameServerInfoNode;
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
            if(m_GClient != null)
            {
                NetService.Instance.CloseServerConnection(m_GClient);
                m_GClient = null;
                m_curGSin = null;
                return true;
            }
            return false;
        }

        public void SentToGameServer(ByteString data)
        {
            m_GClient.Send(data);
        }
    }
}