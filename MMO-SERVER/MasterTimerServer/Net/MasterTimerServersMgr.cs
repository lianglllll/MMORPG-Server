using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using MasterTimerServer.Utils;
using Serilog;
using System.Diagnostics;

namespace MasterTimerServer.Net
{
    public class MasterTimerServersMgr : BaseServersMgr<MasterTimerServersMgr>
    {
        // 初始化相关
        protected override void InitLocalServerInfo()
        {
            // 本服务器的信息
            MasterTimeServerInfoNode mtNode = new();
            m_localSin.ServerType = SERVER_TYPE.Mastertime;
            m_localSin.Ip = Config.Server.ip;
            m_localSin.Port = Config.Server.serverPort;
            m_localSin.ServerId = 0;
            m_localSin.MasterTimeServerInfo = mtNode;
            m_localSin.EventBitmap = 0;

            // 设置一下CC的信息
            m_relatedServerNode[SERVER_TYPE.Controlcenter].ServerInfoNode.Ip = Config.CCConfig.ip;
            m_relatedServerNode[SERVER_TYPE.Controlcenter].ServerInfoNode.Port = Config.CCConfig.port;

        }
        protected override void InitNetwork()
        {
            ConnManager.Instance.Init(Config.Server.workerCount, Config.Server.heartBeatSendInterval, Config.Server.heartBeatCheckInterval, Config.Server.heartBeatTimeOut,
                false, true, true,
                null, 0, null, null,
                Config.Server.ip, Config.Server.serverPort, ClusterServerConnected, ClusterServerDisconnected);
        }
        protected override void InitSpecificComponents()
        {
        }
        protected override void ConnectedCCAndRegisterAfter(RepeatedField<ClusterEventNode> clusterEventNodes)
        {
            // 开始网络监听，预示着当前服务器的正式启动
            ConnManager.Instance.Start();
            Log.Information("\x1b[32m" + "The server is ready." + "\x1b[0m");
        }

        // 其他服务器连接过来的服务器  的 连接事件
        private void ClusterServerConnected(Connection conn)
        {
            Log.Information("A slaveTimeServer connected to masterTimeServer.");
        }
        private void ClusterServerDisconnected(Connection conn)
        {
            Log.Information("A slaveTimeServer disconnected from masterTimeServer.");
        }
    }
}


