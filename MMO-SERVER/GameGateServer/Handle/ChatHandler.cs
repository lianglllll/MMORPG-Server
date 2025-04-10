using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameGateServer.Net;
using HS.Protobuf.Chat;
using HS.Protobuf.Common;
using HS.Protobuf.GameGate;

namespace GameGateServer.Handle
{
    public class ChatHandler : Singleton<ChatHandler>
    {
        public override void Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<SendChatMessageRequest>((int)ChatProtocl.SendChatMessageReq);
            ProtoHelper.Instance.Register<ChatMessageResponse>((int)ChatProtocl.ChatMessageResp);
            // 消息的订阅
            MessageRouter.Instance.Subscribe<SendChatMessageRequest>(_HandleSendChatMessageRequest);
            MessageRouter.Instance.Subscribe<ChatMessageResponse>(_HandleChatMessageResponse);
        }
        public void UnInit()
        {
        }

        private void _HandleSendChatMessageRequest(Connection conn, SendChatMessageRequest message)
        {
            var session = conn.Get<Session>();
            if (session == null)
            {
                goto End;
            }

            // 需要将信息进行分流
            // 1. 附近频道的信息需要发送到scene服务器中处理
            // 2. 其他频道信息发送到game服务器中处理
            if(message.ChatMessage.Channel == ChatMessageChannel.Local)
            {
                ServersMgr.Instance.SendToSceneServer(session.curSceneId, message);
            }
            else
            {
                message.ChatMessage.FromChrId = session.m_cId;
                ServersMgr.Instance.SendToGameServer(message);
            }
        End:
            return;
        }
        private void _HandleChatMessageResponse(Connection conn, ChatMessageResponse message)
        {
            var session = SessionManager.Instance.GetSessionBySessionId(message.SessionId);
            if (session == null)
            {
                goto End;
            }
            message.SessionId = "";
            session.Send(message);
        End:
            return;
        }
    }
}
