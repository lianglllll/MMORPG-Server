using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core;
using GameServer.Core.Model;
using GameServer.Manager;
using GameServer.Net;
using HS.Protobuf.Chat;
using HS.Protobuf.Common;
using HS.Protobuf.ControlCenter;
using HS.Protobuf.Game;
using Serilog;
using System;

namespace GameServer.Handle
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
        
        }
        public void UnInit()
        {
        }

        private void _HandleSendChatMessageRequest(Connection conn, SendChatMessageRequest message)
        {
            ChatMessageV2 chatMessage = message.ChatMessage;

            // 获取当前的sender的info，填充信息
            GameCharacter chr = GameCharacterManager.Instance.GetGameCharacterByCid(chatMessage.FromChrId);            
            if(chr == null)
            {
                goto End;
            }

            // 补全一些信息(减少带宽考虑)
            chatMessage.FromChrName = chr.ChrName;
            var resp = new ChatMessageResponse();

            if (chatMessage.Channel == ChatMessageChannel.Private)
            {
                Log.Warning("没实现");
            }
            else if(chatMessage.Channel == ChatMessageChannel.Scene)
            {
                resp.ChatMessages.Add(chatMessage);
                chatMessage.SceneId = chr.CurSceneId;
                foreach(var sChr in GameCharacterManager.Instance.GetPartGameCharacterBySceneId(chr.CurSceneId).Values)
                {
                    resp.SessionId = sChr.SessionId;
                    sChr.SendToGate(resp);
                }
            }
            else if (chatMessage.Channel == ChatMessageChannel.World)
            {
                resp.ChatMessages.Add(chatMessage);
                foreach (var sChr in GameCharacterManager.Instance.GetAllGameCharacter().Values)
                {
                    resp.SessionId = sChr.SessionId;
                    sChr.SendToGate(resp);
                }
            }
            else if (chatMessage.Channel == ChatMessageChannel.System)
            {
                Log.Warning("没实现");
            }
            else if (chatMessage.Channel == ChatMessageChannel.Team)
            {
                Log.Warning("没实现");
            }
            else if (chatMessage.Channel == ChatMessageChannel.Guild)
            {
                Log.Warning("没实现");
            }
            else
            {
                Log.Warning("没实现");
            }

        End:
            return;
        }
    }
}
