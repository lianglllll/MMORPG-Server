using Common.Summer.Core;
using Common.Summer.Tools;
using Google.Protobuf;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Common.Summer.Net
{
    //消息单元
    class Msg
    {
        public Connection conn;//谁发的
        public IMessage message;//消息
    }

    // 消息路由
    public class MessageRouter:Singleton<MessageRouter>
    {
        private int ThreadCount = 1; //线程个数
        private int WorkerCount = 0; //线程工作个数
        bool running = false;        //工作状态
        public bool Running { get { return running; } }

        //通过set每次可以唤醒一个线程
        AutoResetEvent threadEvent = new AutoResetEvent(false);  

        //消息队列，所有客户端发送过来的消息都暂存到这里
        private ConcurrentQueue<Msg> messageQueue = new ConcurrentQueue<Msg>();

        //消息处理委托
        public delegate void MessageHandler<T>(Connection sender, T message);

        //消息频道（技能频道，战斗频道，物品频道）（订阅记录）
        private ConcurrentDictionary<string, Delegate> delegateMap = new ConcurrentDictionary<string, Delegate>();

        public void Start(int ThreadCount)
        {
            if (running) return;
            running = true;
            this.ThreadCount = Math.Min(Math.Max(1, ThreadCount),10);

            for(int i = 0; i < this.ThreadCount; i++)
            {
                //将委托任务交付给线程池
                ThreadPool.QueueUserWorkItem(new WaitCallback(MessageWork));
            }

            //等待一会,让全部线程
            while (WorkerCount < this.ThreadCount)
            {
                Thread.Sleep(100);
            }
        }
        public void Stop()
        {
            running = false;
            messageQueue.Clear();
            //等待全部线程下线
            while (WorkerCount > 0)
            {
                threadEvent.Set();//防止线程一直处于阻塞状态
            }
            Thread.Sleep(50);
        }


        public void Subscribe<T>(MessageHandler<T> handler) where T : Google.Protobuf.IMessage
        {
            string type = typeof(T).FullName;
            
            if (!delegateMap.ContainsKey(type))//没有就创建一个空的
            {
                delegateMap[type] = null;
            }

            //添加订阅者,因为这里它不知道是什么类型的委托，所以要转型（这里是委托链注意，可能会多次绑定）
            delegateMap[type] = (MessageHandler < T >)delegateMap[type]  + handler;
        }
        public void UnSubscribe<T>(MessageHandler<T> handler) where T : Google.Protobuf.IMessage
        {
            string key = typeof(T).FullName;
            if (!delegateMap.ContainsKey(key))
            {//频道不存在给你加一个
                delegateMap[key] = null;
            }
            //添加订阅者,因为这里它不知道是什么类型的委托，所以要转型
            delegateMap[key] = (MessageHandler<T>)delegateMap[key] - handler;
        }


        // 添加消息到消息队列
        public void AddMessage(Connection sender, Google.Protobuf.IMessage message)
        {
            //加锁
            lock (messageQueue)
            {
                messageQueue.Enqueue(new Msg() { conn = sender, message = message });
            }
            //唤醒一个进程来处理消息队列
            threadEvent.Set();
        }
        // 多线程消息处理
        private void MessageWork(object state)
        {
            try
            {
                //考虑到线程安全
                Interlocked.Increment(ref WorkerCount); //WorkerCount + 1

                //线程执行的程序
                while (running)
                {
                    //这里有可能，例如说4个线程同时跳过了这个if语句
                    if (messageQueue.Count == 0)
                    {
                        //线程会在这里阻塞
                        threadEvent.WaitOne();
                        continue;//防止醒过来的时候又没消息了（或者stop），再走一遍流程
                    }
                    //从消息队列中取出一个消息
                    Msg msg = null;
                    lock (messageQueue)
                    {
                        if (messageQueue.Count == 0) continue;
                        messageQueue.TryDequeue(out msg);
                    }
                    IMessage package = msg.message;

                    //判断这个包是什么类型
                    if (package != null)
                    {
                        //处理这个数据包
                        executeMessage(msg.conn, package);//将package向下传递
                    }
                }              
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                Interlocked.Decrement(ref WorkerCount);//WorkerCount-1
            }
            Console.WriteLine("MessageWork thread end");
            
        }
        private void executeMessage(Connection sender, IMessage message)
        {
            var fullName = message.GetType().FullName;
            if(delegateMap.TryGetValue(fullName,out var handler))
            {
                try
                {
                    handler.DynamicInvoke(sender, message);
                }catch(Exception e)
                {
                    Log.Error("[MessageRouter.executeMessage]" + e.StackTrace);
                }
            }
        }

        #region 弃用
        /*
        /// <summary>
        /// 触发相对应订阅的事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void Fire<T>(Connection sender, T msg)
        {

            string type = typeof(T).FullName;
            //Console.WriteLine(type);
            //没人订阅自然就不需要处理这个消息了
            if (delegateMap.ContainsKey(type))
            {
                //Console.WriteLine(type);
                MessageHandler<T> handler = (MessageHandler<T>)delegateMap[type];
                try
                {
                    handler?.Invoke(sender, msg);
                }
                catch (Exception e)
                {
                    Console.WriteLine("MessageRouter.Fire error:" + e.StackTrace);
                }
            }

        }

        */
        #endregion
    }
}
