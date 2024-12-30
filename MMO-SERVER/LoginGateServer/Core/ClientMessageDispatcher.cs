using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.LoginGate;
using LoginGateServer.Net;

namespace LoginGateServer.Core
{
    public class ClientMessageDispatcher : Singleton<ClientMessageDispatcher>
    {
        // 转发 
        public void Init()
        {
            ProtoHelper.Register<LGEnvelope>((int)LoginGateProtocl.Lgenvelope);
            MessageRouter.Instance.Subscribe<LGEnvelope>(_ProcessEnvelope);
        }
        public void UnInit()
        {

        }

        private void _ProcessEnvelope(Connection sender, LGEnvelope message)
        {
            // 目前较为简单，简单验证一下协议号，直接转发
            if(message.ProtocolCode%10000 == 2)
            {
                _DispatchMessage(message.Data);
            }
        }
        private void _DispatchMessage(ByteString data)
        {
            ServersMgr.Instance.SentToLoginServer(data);
        }
    }
}
