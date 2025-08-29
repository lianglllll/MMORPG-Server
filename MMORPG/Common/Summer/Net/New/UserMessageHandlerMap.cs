using Common.Summer.Tools;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using static Common.Summer.Net.MessageRouter;

namespace Common.Summer.Net
{
    public class UserMessageHandlerArgs2
    {
        public int clientId;
        public int seqId;
    }

    public class UserMessageHandlerMap : Singleton<UserMessageHandlerMap>
    {
        public delegate IMessage MessageHandler<T>(UserMessageHandlerArgs2 args, T message);

        private readonly ConcurrentDictionary<string, Delegate> delegateMap = new ConcurrentDictionary<string, Delegate>();

        public void Subscribe<T>(MessageHandler<T> handler) where T : IMessage
        {
            string type = typeof(T).FullName;
            if (!delegateMap.ContainsKey(type))//没有就创建一个空的
            {
                delegateMap[type] = null;
            }
            delegateMap[type] = (MessageHandler<T>)delegateMap[type] + handler;
        }

        public void UnSubscribe<T>(MessageHandler<T> handler) where T : IMessage
        {
            string key = typeof(T).FullName;
            if (!delegateMap.ContainsKey(key))
            {//频道不存在给你加一个
                delegateMap[key] = null;
            }
            //添加订阅者,因为这里它不知道是什么类型的委托，所以要转型
            delegateMap[key] = (MessageHandler<T>)delegateMap[key] - handler;
        }

        public Delegate GetMessageHandler(string key)
        {
            delegateMap.TryGetValue(key, out var handler);
            return handler;
        }
    }
}