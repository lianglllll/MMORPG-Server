using Common.Summer.Core;
using Common.Summer.Tools;
using Google.Protobuf.Collections;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Common.Summer.Net
{

    public class ServerNode
    {
        public ServerInfoNode ServerInfoNode { get; set; }
        public NetClient Client { get; set; }
        public Stopwatch SW = null;
    }

    public abstract class BaseServersMgr<T> :Singleton<T> where T : BaseServersMgr<T>, new() 
    {
        protected ServerInfoNode m_localSin;
        protected Dictionary<SERVER_TYPE, ServerNode> m_relatedServerNode;

        #region 初始化相关
        public override void Init()
        {
            m_localSin = new ServerInfoNode();
            m_relatedServerNode = new Dictionary<SERVER_TYPE, ServerNode>();
            m_relatedServerNode.Add(SERVER_TYPE.Controlcenter, new ServerNode
            {
                ServerInfoNode = new ServerInfoNode()
            }); 

            // 这里使用模版方法
            InitLocalServerInfo();
            InitNetwork();
            RegisterProtocols();
            SubscribeMessageReqs();
            InitSpecificComponents();

            // 连接到CC
            ConnectToCC();
        }
        protected abstract void InitLocalServerInfo();
        protected virtual int SetEventBitmap() { 
            return 0;
        }
        protected abstract void InitNetwork();
        protected virtual void RegisterProtocols()
        {
            ProtoHelper.Instance.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Instance.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);
        }
        protected virtual void SubscribeMessageReqs()
        {
            MessageRouter.Instance.Subscribe<ServerInfoRegisterResponse>(_HandleServerInfoRegisterResponse);
        }
        protected virtual void InitSpecificComponents() { }

        #endregion

        #region 连接到cc

        public void ConnectToCC()
        {
            var ccNode = m_relatedServerNode[SERVER_TYPE.Controlcenter];
            ConnManager.Instance.ConnctToServer(ccNode.ServerInfoNode.Ip, ccNode.ServerInfoNode.Port, 
                _CCConnectedCallback, _CCConnectedFailedCallback, _CCDisconnectedCallback);
        }
        private void _CCConnectedCallback(NetClient tcpClient)
        {
            var ccNode = m_relatedServerNode[SERVER_TYPE.Controlcenter];
            ccNode.Client = tcpClient;
            Log.Information("Successfully connect to the controlCenter server({0},{1})", ccNode.ServerInfoNode.Ip, ccNode.ServerInfoNode.Port);

            // 向cc注册自己
            ccNode.SW = Stopwatch.StartNew();
            ServerInfoRegisterRequest req = new();
            req.ServerInfoNode = m_localSin;
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
            Log.Error("Disconnect from the ControlCenter server, attempting to reconnect");
            var ccNode = m_relatedServerNode[SERVER_TYPE.Controlcenter];
            ccNode.Client = null;
            ConnectToCC();
        }
        private void _HandleServerInfoRegisterResponse(Connection conn, ServerInfoRegisterResponse message)
        {
            if (message.ResultCode == 0)
            {
                m_localSin.ServerId = message.ServerId;
                var ccNode = m_relatedServerNode[SERVER_TYPE.Controlcenter];
                long comsumTime = ccNode.SW.ElapsedMilliseconds;
                Log.Information("Successfully registered to ControlCenter(Time-consuming {0}ms), Get serverId = [{1}]", comsumTime, message.ServerId);
                Log.Information("Subscription events: {0}", message.ClusterEventNodes);
                ConnectedCCAndRegisterAfter(message.ClusterEventNodes);
            }
            else
            {
                Log.Error(message.ResultMsg);
            }
        }
        #endregion

        // 流程
        protected virtual void ConnectedCCAndRegisterAfter(RepeatedField<ClusterEventNode> clusterEventNodes)
        {

        }
    }
}
