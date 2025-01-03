using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Login;
using LoginServer.Net;
using Serilog;
using System.Collections;

namespace LoginServer.Core
{
    public class LoginServerHandler : Singleton<LoginServerHandler>
    {
        public void Init()
        {
            // 协议注册
            ProtoHelper.Register<ClusterEventResponse>((int)ControlCenterProtocl.ClusterEventResp);
            ProtoHelper.Register<TCPEnvelope>((int)CommonProtocl.TcpEnvelope);

            // 消息的订阅
            MessageRouter.Instance.Subscribe<ClusterEventResponse>(_HandleClusterEventResponse);
            MessageRouter.Instance.Subscribe<TCPEnvelope>(_HandleTCPEnvelope);

            // test
            ProtoHelper.Register<UserLoginRequest>((int)LoginProtocl.UserLoginRequest);
            ProtoHelper.Register<UserLoginResponse>((int)LoginProtocl.UserLoginResponse);
            UserMessageHandlerMap.Instance.Subscribe<UserLoginRequest>(_HandleUserLoginRequest);
        }

        private IMessage _HandleUserLoginRequest(UserMessageHandlerArgs2 args, UserLoginRequest message)
        {
            Log.Debug("UserLoginRequest");
            return new UserLoginResponse { ResultCode = 0, ResultMsg = "hhh" };
        }

        public void UnInit()
        {

        }

        private void _HandleClusterEventResponse(Connection sender, ClusterEventResponse message)
        {
            if (message.ClusterEventNode.EventType == ClusterEventType.DbproxyEnter)
            {
                Log.Debug("A new DBProxy Server has joined the cluster.");
                ServersMgr.Instance.AddDBServerInfo(message.ClusterEventNode.ServerInfoNode);
            }
        }
        private void _HandleTCPEnvelope(Connection sender, TCPEnvelope tcpEnvelopeMsg)
        {
            // 解析bytes获取reqMsg和处理函数
            IMessage reqMsg = ProtoHelper.BytesParse2IMessage(tcpEnvelopeMsg.Data.ToByteArray(), out var t);
            var handler = UserMessageHandlerMap.Instance.GetMessageHandler(t.FullName);
            // 执行处理函数
            IMessage respMsg = (IMessage) handler.DynamicInvoke(new UserMessageHandlerArgs2 { clientId = tcpEnvelopeMsg.ClientId, seqId = tcpEnvelopeMsg.SeqId}, reqMsg);
            var bytes = ProtoHelper.IMessageParse2BytesNoLen(respMsg);
            // 回包
            tcpEnvelopeMsg.Data = ByteString.CopyFrom(bytes);
            sender.Send(tcpEnvelopeMsg);
        }

    }
}