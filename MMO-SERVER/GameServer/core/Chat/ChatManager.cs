using GameServer.Model;
using Google.Protobuf.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.Summer.Tools;
using HS.Protobuf.Chat;
using System.Net.NetworkInformation;
using Common.Summer.Core;
using System;

namespace GameServer.Manager
{
    public class ChatManager: Singleton<ChatManager>
    {
        private int LocalMsgCacheNum = 60;
        private int MaxChatRecoredNums = 10;
        private List<ChatMessageV2>                            System                  = new();
        private ConcurrentDictionary<int, List<ChatMessageV2>> sceneChatMessageQueues  = new();
        private List<ChatMessageV2>                            World                   = new();
        private ConcurrentDictionary<int, List<ChatMessageV2>> Team                    = new();
        private ConcurrentDictionary<int, List<ChatMessageV2>> Guild                   = new();          

        private ConcurrentQueue<ChatMessageV2> HandleChatMessageQueue = new();
        public override void Init()
        {
            //Scheduler.Instance.Update(Update);
        }
        private void Update()
        {
            if (HandleChatMessageQueue.Count <= 0) return;

            foreach(var chatMsg in HandleChatMessageQueue)
            {
                HandleWorldChatMessage(chatMsg);
            }
        }
        private void HandleWorldChatMessage(ChatMessageV2 message)
        {

        }
        private void HandleSceneChatMessage(ChatMessageV2 message)
        {

        }

        public void AddWorldChatMessage(ChatMessageV2 message)
        {

        }
        public void AddSceneChatMessage(int sceneId, ChatMessageV2 message) { 
        
        }
    }
}
