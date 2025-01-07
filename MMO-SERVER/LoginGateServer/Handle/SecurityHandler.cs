using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.Common;

namespace LoginGateServer.Handle
{
    public class SecurityHandler : Singleton<SecurityHandler>
    {
        public bool Init()
        {
            ProtoHelper.Instance.Register<ExchangePublicKeyRequest>((int)CommonProtocl.ExchangePublicKeyReq);
            ProtoHelper.Instance.Register<ExchangePublicKeyResponse>((int)CommonProtocl.ExchangePublicKeyResp);
            ProtoHelper.Instance.Register<ExchangeCommunicationSecretKeyRequest>((int)CommonProtocl.ExchangeCommunicationSecretKeyReq);
            ProtoHelper.Instance.Register<ExchangeCommunicationSecretKeyResponse>((int)CommonProtocl.ExchangeCommunicationSecretKeyResp);

            MessageRouter.Instance.Subscribe<ExchangePublicKeyRequest>(_HandleExchangePublicKeyRequest);
            MessageRouter.Instance.Subscribe<ExchangeCommunicationSecretKeyRequest>(_HandleExchangeCommunicationSecretKeyRequest);


            return true;
        }
        public bool UnInit()
        {
            return true;
        }

        private void _HandleExchangePublicKeyRequest(Connection sender, ExchangePublicKeyRequest message)
        {
            sender.m_encryptionManager.SetRemoteRsaPublicKey(message.ClientPublicKey);
            ExchangePublicKeyResponse resp = new();
            resp.ServerPublilcKey = sender.m_encryptionManager.GetLocalRsaPublicKey();
            sender.Send(resp);
        }

        private void _HandleExchangeCommunicationSecretKeyRequest(Connection sender, ExchangeCommunicationSecretKeyRequest message)
        {
            string k1 = sender.m_encryptionManager.RsaDecrypt(message.Key1);
            string k2 = sender.m_encryptionManager.RsaDecrypt(message.Key2);
            sender.m_encryptionManager.SetAesKeyAndIv(k1, k2);

            ExchangeCommunicationSecretKeyResponse resp = new();
            resp.ResultCode = 0;
            sender.Send(resp);
        }
    }
}
