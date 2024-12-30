using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.LoginGate;
using GameGateServer.Net;
using HS.Protobuf.GameGate;

namespace GameGateServer.Core
{
    public class ClientMessageDispatcher : Singleton<ClientMessageDispatcher>
    {
        // 转发 
        public void Init()
        {
            ProtoHelper.Register<GGEnvelope>((int)GameGateProtocl.Ggenvelope);
            MessageRouter.Instance.Subscribe<GGEnvelope>(_ProcessEnvelope);
        }
        public void UnInit()
        {

        }

        private void _ProcessEnvelope(Connection sender, GGEnvelope message)
        {
            // 目前较为简单，简单验证一下协议号，直接转发
            if(message.ProtocolCode%10000 == 3)
            {
                _DispatchMessage(message.Data);
            }
        }
        private void _DispatchMessage(ByteString data)
        {
            ServersMgr.Instance.SentToGameServer(data);
        }
    }
}
