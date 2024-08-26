using GameServer.Model;
using Google.Protobuf.Collections;
using Proto;
using Summer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
    public class ChatManager : Singleton<ChatManager>
    {
        public List<ChatMessage> System = new List<ChatMessage>();
        public List<ChatMessage> World = new List<ChatMessage>();
        public ConcurrentDictionary<int, List<ChatMessage>> Local = new ConcurrentDictionary<int, List<ChatMessage>>();     //本地，int是当前场景的space ID
        public ConcurrentDictionary<int, List<ChatMessage>> Team = new ConcurrentDictionary<int, List<ChatMessage>>();      //int 是team ID   
        public ConcurrentDictionary<int, List<ChatMessage>> Guild = new ConcurrentDictionary<int, List<ChatMessage>>();     //int 是Guild ID     
        public int LocalMsgCacheNum = 60;

        //一组get set方法
        public void AddMessage(Character chr, ChatMessage message)
        {

            switch (message.Channel)
            {
                case ChatChannel.Local:
                    this.AddLocalMessage(chr.CurSpaceId, message);         
                    break;
                case ChatChannel.World:
                    //this.AddWorldMessage(message);
                    break;
                case ChatChannel.System:
                    //this.AddSystemMessage(message);
                    break;
                case ChatChannel.Team:
                    //this.AddTeamMessage(chr.TeamId, message);
                    break;
                case ChatChannel.Guild:
                    //this.AddGuildMessage(chr.GuildId, message);
                    break;
            }


        }
        private void AddSystemMessage(ChatMessage message)
        {
            System.Add(message);
        }
        private void AddWorldMessage(ChatMessage message)
        {
            World.Add(message);
        }
        private void AddLocalMessage(int spaceId, ChatMessage message)
        {
            List<ChatMessage> messages = null;
            lock (Local)
            {
                //没有就创建
                if (!this.Local.TryGetValue(spaceId, out messages))
                {
                    messages = new List<ChatMessage>();
                    this.Local[spaceId] = messages;
                }
            }

            //添加消息
            lock (Local[spaceId])
            {
                messages.Add(message);//添加单条信息
                if(messages.Count >= LocalMsgCacheNum)
                {
                    messages.RemoveRange(0, LocalMsgCacheNum / 2);
                }

            }

        }

        /// <summary>
        /// 获取本space的信息
        /// </summary>
        /// <param name="spaceId"></param>
        /// <param name="index"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public int GetLocalMessages(int spaceId,int index,RepeatedField<ChatMessage> result)
        {
            if(!this.Local.TryGetValue(spaceId,out List<ChatMessage> messages))
            {
                return 0;
            }
            return GetNewMessages(index, result, messages);
        }

        /// <summary>
        /// Chat获取新消息
        /// </summary>
        int MaxChatRecoredNums = 10;
        private int GetNewMessages(int index, RepeatedField<ChatMessage> result, List<ChatMessage> messages)
        {
            if(index == 0)
            {
                if(messages.Count > MaxChatRecoredNums)
                {
                    index = messages.Count - MaxChatRecoredNums;
                }
            }

            for(;index < messages.Count; index++)
            {
                result.Add(messages[index]);
            }
            return index;
        }

    }
}
