using System.Net;
using Serilog;
using Common.Summer.Tools;
using Common.Summer.Net;
using Common.Summer.Core;
using ControlCenter.Utils;
using HS.Protobuf.Common;
using ControlCenter.Core;

namespace ControlCenter.Net
{
    /// <summary>
    /// 网络服务
    /// </summary>
    public class NetService : Singleton<NetService>
    {
        private TcpServer? m_tcpServer;
        private float m_heartBeatTimeOut;
        private Dictionary<Connection, DateTime> m_lastHeartbeatTimes = new();

        public void Init()
        {
            // 启动消息分发器
            MessageRouter.Instance.Start(Config.Server.workerCount);
            ProtoHelper.Instance.Init();

            // proto注册
            ProtoHelper.Instance.Register<SSHeartBeatRequest>((int)CommonProtocl.SsHeartbeatReq);
            ProtoHelper.Instance.Register<SSHeartBeatResponse>((int)CommonProtocl.SsHeartbeatResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<SSHeartBeatRequest>(_SSHeartBeatRequest);

            // 启动网络监听
            m_tcpServer = new TcpServer();
            m_tcpServer.Init(Config.Server.ip, Config.Server.port, 100, _OnConnected, _OnDisconnected);

            // 定时检查心跳包的情况
            m_heartBeatTimeOut = Config.Server.heartBeatTimeOut;
            Scheduler.Instance.AddTask(_CheckHeatBeat, Config.Server.heartBeatCheckInterval, 0);
        }
        public void UnInit()
        {
            m_tcpServer.UnInit();
            m_tcpServer = null;
            m_lastHeartbeatTimes.Clear();
        }

        private void _OnConnected(Connection conn)
        {
            try
            {
                if (conn.Socket != null && conn.Socket.Connected)
                {
                    var ipe = conn.Socket.RemoteEndPoint;
                    Log.Debug("[连接成功]" + IPAddress.Parse(((IPEndPoint)ipe).Address.ToString()) + " : " + ((IPEndPoint)ipe).Port.ToString());
                    // 给conn添加心跳时间
                    m_lastHeartbeatTimes[conn] = DateTime.Now;
                }
                else
                {
                    Log.Warning("[NetService]尝试访问已关闭的 Socket 对象");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.Error("[NetService]Socket 已被释放: " + ex.Message);
            }
        }
        private void _OnDisconnected(Connection conn)
        {
            if (conn == null) return;

            //从心跳字典中删除连接
            if (m_lastHeartbeatTimes.ContainsKey(conn))
            {
                m_lastHeartbeatTimes.Remove(conn);
            }

            int serverId = conn.Get<int>();
            if (serverId > 0) { 
                ServersMgr.Instance.OnDisconnected(serverId);
            }
        }
        private void _SSHeartBeatRequest(Connection conn, SSHeartBeatRequest message)
        {
            //更新心跳时间
            m_lastHeartbeatTimes[conn] = DateTime.Now;

            //响应
            SSHeartBeatResponse resp = new SSHeartBeatResponse();
            conn.Send(resp);
        }
        private void _CheckHeatBeat()
        {
            // Log.Debug($"心跳检测，当前连接数：{m_lastHeartbeatTimes.Count}");
            DateTime nowTime = DateTime.Now;
            //这里规定心跳包超过m_lastHeartbeatTimes秒没用更新就将连接清理
            foreach (var kv in m_lastHeartbeatTimes)
            {
                TimeSpan gap = nowTime - kv.Value;
                if (gap.TotalSeconds > m_heartBeatTimeOut)
                {
                    Log.Debug("心跳超时");
                    Connection conn = kv.Key;
                    _OnDisconnected(conn);

                    // 转交给下一层的connection去进行关闭
                    conn?.CloseConnection();
                }
            }
        }
    }
}
