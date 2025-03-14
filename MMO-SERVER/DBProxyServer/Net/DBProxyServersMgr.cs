using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using DBProxyServer.Handle;
using DBProxyServer.Utils;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;

namespace DBProxyServer.Net
{
    public class DBProxyServersMgr : BaseServersMgr<DBProxyServersMgr>
    {
        protected override void InitLocalServerInfo()
        {
            DBProxyServerInfoNode dbpNode = new();
            m_localSin.ServerType = SERVER_TYPE.Dbproxy;
            m_localSin.Ip = Config.Server.ip;
            m_localSin.Port = Config.Server.serverPort;
            m_localSin.ServerId = 0;
            m_localSin.DbProxyServerInfo = dbpNode;
            m_localSin.EventBitmap = SetEventBitmap();

            // 设置一下CC的信息
            m_relatedServerNode[SERVER_TYPE.Controlcenter].ServerInfoNode.Ip = Config.CCConfig.ip;
            m_relatedServerNode[SERVER_TYPE.Controlcenter].ServerInfoNode.Port = Config.CCConfig.port;
        }
        protected override int SetEventBitmap()
        {
            int bitmap = 0;
            List<ClusterEventType> events = new List<ClusterEventType>
            {
                ClusterEventType.MastertimeEnter,
            };
            foreach (var e in events)
            {
                bitmap |= (1 << (int)e);
            }
            return bitmap;
        }
        protected override void InitNetwork()
        {
            // 网络服务开启
            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                false, true, true,
                null, 0, null, null,
                Config.Server.ip, Config.Server.serverPort, null, null);
        }
        protected override void RegisterProtocols()
        {
            base.RegisterProtocols();
            // 下面这个协议是重复注册过的，原因是时序问题会报错，我看得不舒服。
            ProtoHelper.Instance.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
        }
        protected override void InitSpecificComponents()
        {
            base.InitSpecificComponents();
            Scheduler.Instance.Start(Config.Server.updateHz);
            UserHandler.Instance.Init();
            CharacterHandler.Instance.Init();
            WorldHandler.Instance.Init();
            DBProxyServerHandler.Instance.Init();
        }
        protected override void ConnectedCCAndRegisterAfter(RepeatedField<ClusterEventNode> clusterEventNodes)
        {
            foreach (var node in clusterEventNodes)
            {
                if (node.EventType == ClusterEventType.MastertimeEnter)
                {
                    AddMTServerInfo(node.ServerInfoNode);
                }
            }
            Parse1();
        }

        // 流程
        private void Parse1()
        {
            // 连接时间同步服务器
        }
        private void Parse2()
        {
            // 满足了全部条件才会进入下一个阶段
            var mtNode = m_relatedServerNode[SERVER_TYPE.Mastertime];
            if (mtNode.Client == null)
            {
                goto End;
            }

            // 开始网络监听，预示着当前服务器的正式启动
            ConnManager.Instance.Start();
            Log.Information("\x1b[32m" + "The server is ready." + "\x1b[0m");

        End:
            return;
        }

        // MT
        public void AddMTServerInfo(ServerInfoNode serverInfoNode)
        {
            if (!m_relatedServerNode.ContainsKey(SERVER_TYPE.Mastertime))
            {
                var sNode = new ServerNode();
                sNode.ServerInfoNode = serverInfoNode;
                m_relatedServerNode[SERVER_TYPE.Mastertime] = sNode;
                _ConnectToMT();
            }
        }
        private void _ConnectToMT()
        {
            ServerInfoNode node = m_relatedServerNode[SERVER_TYPE.Mastertime].ServerInfoNode;
            ConnManager.Instance.ConnctToServer(node.Ip, node.Port,
                _MTConnectedCallback, _MTConnectedFailedCallback, _MTDisconnectedCallback);
        }
        private void _MTConnectedCallback(NetClient tcpClient)
        {
            Log.Information("Successfully connected to the MT server[{0}].", m_relatedServerNode[SERVER_TYPE.Mastertime].ServerInfoNode.ServerId);
            // 记录
            m_relatedServerNode[SERVER_TYPE.Mastertime].Client = tcpClient;
            Parse2();
        }
        private void _MTConnectedFailedCallback(NetClient tcpClient, bool isEnd)
        {
            if (isEnd)
            {
                Log.Error("Connect to MT server failed, the server may not be turned on");
            }
            else
            {
                //做一下重新连接
                Log.Error("Connect to MT server failed, attempting to reconnect");
                Log.Error("重连还没写");
            }

        }
        private void _MTDisconnectedCallback(NetClient tcpClient)
        {
        }
    }
}