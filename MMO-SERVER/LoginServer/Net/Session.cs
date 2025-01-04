using Common.Summer.Core;
using HS.Protobuf.DBProxy.DBUser;

namespace LoginServer.Net
{
    /// <summary>
    /// 用户会话,代表玩家客户端，只有登录成功的用户才分配给他session
    /// </summary>
    public class Session
    {
        public string Id { get; private set; }
        public DBUserNode dbUser { get; set; }

        public Session(string sessionId)
        {
            Id = sessionId;
        }

    }
}
