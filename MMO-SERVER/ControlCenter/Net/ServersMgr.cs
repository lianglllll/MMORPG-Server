using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using Serilog;

namespace ControlCenter.Core
{
    public class ServersMgr : Singleton<ServersMgr>
    {
        private Dictionary<int, ServerInfoNode> m_servers = new();
        private IdGenerator idGenerator = new();

        public bool Init()
        {
            // proto注册
            ProtoHelper.Register<ServerInfoRegisterRequest>((int)ControlCenterProtocl.ServerinfoRegisterReq);
            ProtoHelper.Register<ServerInfoRegisterResponse>((int)ControlCenterProtocl.ServerinfoRegisterResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<ServerInfoRegisterRequest>(_ServerInfoRegisterRequest);

            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        public bool OnDisconnected(int serverId)
        {
            if (m_servers.ContainsKey(serverId))
            {
                m_servers.Remove(serverId);
                idGenerator.ReturnId(serverId);
                return true;
            }
            return false;
        }
        private void _ServerInfoRegisterRequest(Connection conn, ServerInfoRegisterRequest message)
        {
            ServerInfoRegisterResponse resp = new();
            switch (message.ServerInfoNode.ServerType)
            {
                case SERVER_TYPE.Login:

                    break;
                case SERVER_TYPE.Logingate:

                    break;
                case SERVER_TYPE.Logingatemgr:

                    break;
                case SERVER_TYPE.Game:

                    break;
                case SERVER_TYPE.Gamegate:

                    break;
                case SERVER_TYPE.Gamegatemgr:

                    break;
                case SERVER_TYPE.Scene:

                    break;
                case SERVER_TYPE.Dbproxy:

                    break;
                default:
                    Log.Error("NetService._ServerInfoRegisterRequest 错误的注册类型");
                    resp.ResultCode = 1;
                    resp.ResultMsg = "错误的注册类型";
                    goto End;
            }
            int serverId = idGenerator.GetId();
            ServerInfoNode node = message.ServerInfoNode;
            node.ServerId = serverId;
            m_servers.Add(serverId, node);
            conn.Set<int>(serverId);//方便conn断开时找

            resp.ResultCode = 0;
            resp.ResultMsg = "注册成功";
            resp.ServerId = serverId;
            Log.Information($"[ServerInfoRegister] 注册成功，{node.ToString()}");
        End:
            conn.Send(resp);
        }

        // test
        public Dictionary<int, ServerInfoNode> GetServers()
        {
            return m_servers;
        }
    }
}
