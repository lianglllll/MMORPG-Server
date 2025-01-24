

using Common.Summer.Core;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using LoginGateServer.Net;
using Serilog;

namespace LoginServer.Core
{
    public class LoginServerMonitor : Singleton<LoginServerMonitor>
    {
        public bool Init()
        {
            return true;
        }

        public string RegisterLoginGateInstance(Connection conn, ServerInfoNode serverInfoNode)
        {
            Log.Information($"[serverId = {serverInfoNode.ServerId}], LoginGate Register...");
            // 分配一下连接token
            LoginToken token = LoginTokenManager.Instance.NewToken(conn, serverInfoNode);
            conn.Set<LoginToken>(token);
            return token.Id;
        }
        public void HaveLoginGateInstanceDisconnect(Connection conn)
        {
            // token回收
            var token = conn.Get<LoginToken>();
            if (token != null)
            {
                Log.Error($"[serverId = {token.ServerInfoNode.ServerId}], LoginGate Disaconnet...");
                LoginTokenManager.Instance.RemoveToken(conn.Get<LoginToken>().Id);
            }
        }

    }
}
