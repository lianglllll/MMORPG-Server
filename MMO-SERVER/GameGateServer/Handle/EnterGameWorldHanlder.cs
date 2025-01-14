using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using Google.Protobuf;
using HS.Protobuf.GameGate;
using HS.Protobuf.Login;

namespace GameGateServer.Handle
{
    public class EnterGameWorldHanlder : Singleton<EnterGameWorldHanlder>
    {
        private IdGenerator m_idGenerator = new IdGenerator();
        private Dictionary<int, IMessage> m_tasks = new Dictionary<int, IMessage>();

        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<RegisterSessionToGGRequest>((int)GameGateProtocl.RegisterSessionToGgReq);
            ProtoHelper.Instance.Register<RegisterSessionToGGResponse>((int)GameGateProtocl.RegisterSessionToGgResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<RegisterSessionToGGRequest>(_HandleRegisterSessionToGGRequest);

            return true;
        }

        public bool UnInit()
        {
            return true;
        }

        private void _HandleRegisterSessionToGGRequest(Connection conn, RegisterSessionToGGRequest message)
        {
            SessionManager.Instance.NewSession(message.Session);
        }
    }
}
