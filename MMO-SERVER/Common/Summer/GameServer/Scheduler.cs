using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Summer
{


    //时间单位
    public enum TimeUnit
    {
        Milliseconds,
        Seconds,
        Minutes,
        Hours,
        Days
    }

    //中心计时器
    public class Scheduler : Singleton<Scheduler>
    {

        private List<Task> tasks = new List<Task>();                                        // 任务队列
        private ConcurrentQueue<Task> _addQueue = new ConcurrentQueue<Task>();              // 新增任务队列
        private ConcurrentQueue<Action> _removeQueue = new ConcurrentQueue<Action>();       // 移除任务队列
        private Timer timer;                                                                // 计时器
        private int fps = 50;                                                               // 每秒帧数
        private object tasksLock = new object();                                            // 用于保护任务列表的锁对象

        /// <summary>
        /// 构造方法
        /// </summary>
        public Scheduler()
        {

        }

        /// <summary>
        /// 启动中心计算调度器
        /// </summary>
        public void Start()
        {
            if (timer != null) return;
            //系统线程池中拿一个来用的
            timer = new Timer(new TimerCallback(Execute), null, 0, 1);//每隔一毫秒触发
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            if (timer == null) return;
            // 释放定时器资源
            timer.Dispose();
            timer = null;
            Console.WriteLine("Timer stopped.");
        }

        //下一帧执行的时间
        private long _next = 0;

        /// <summary>
        /// 计时器主循环
        /// </summary>
        private void Execute(object state)
        {
            // tick间隔
            int interval = 1000 / fps;
            long time = GetCurrentTime();
            if (time < _next) return;
            _next = time + interval;

            Time.Tick();

            //处理逻辑帧
            lock (tasks)
            {
                //移除队列
                while (_removeQueue.TryDequeue(out var item))
                {
                    tasks.RemoveAll(task => task.TaskMethod == item);
                }

                //移除完毕的任务
                tasks.RemoveAll(task => task.Completed);

                //添加队列任务
                while (_addQueue.TryDequeue(out var item))
                {
                    tasks.Add(item);
                }


                //执行任务
                foreach(Task task in tasks)
                {

                    if (task.ShouldRun())
                    {
                        task.Run();
                    }
                }


            }
        }


        //给计时器添加一个任务，有一个委托，执行的时间间隔，重复次数（0的话就是不断重复），以秒为间隔
        public void AddTask(Action taskMethod, float seconds, int repeatCount = 0)
        {
            this.AddTask(taskMethod, (int)(seconds * 1000), TimeUnit.Milliseconds, repeatCount);
        }

        //timeUnit时间单位
        public void AddTask(Action taskMethod, int timeValue, TimeUnit timeUnit, int repeatCount = 0)
        {
            int interval = GetInterval(timeValue, timeUnit);
            long startTime = GetCurrentTime() + interval;
            Task task = new Task(taskMethod, startTime, interval, repeatCount);
            _addQueue.Enqueue(task);
        }

        //delay，interval都是以秒为单位的
        public void AddTask(Action taskMethod, float delay, float interval, int repeatCount = 0)
        {
            int _interval = (int)(interval * 1000);
            long startTime = GetCurrentTime() + (long)(delay * 1000);
            Task task = new Task(taskMethod, startTime, _interval, repeatCount);
            _addQueue.Enqueue(task);
        }


        //从任务队列中删除该任务
        public void RemoveTask(Action taskMethod)
        {
            _removeQueue.Enqueue(taskMethod);
        }

        //每个帧都运行的任务
        public void Update(Action action)
        {
            Task task = new Task(action, 0, 0, 0);
            _addQueue.Enqueue(task);
        }



        public static long GetCurrentTime()
        {
            // 获取从1970年1月1日午夜（也称为UNIX纪元）到现在的毫秒数
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        //转化成响应的时间单位数值，都转成毫秒
        private int GetInterval(int timeValue, TimeUnit timeUnit)
        {
            switch (timeUnit)
            {
                case TimeUnit.Milliseconds:
                    return timeValue;
                case TimeUnit.Seconds:
                    return timeValue * 1000;
                case TimeUnit.Minutes:
                    return timeValue * 1000 * 60;
                case TimeUnit.Hours:
                    return timeValue * 1000 * 60 * 60;
                case TimeUnit.Days:
                    return timeValue * 1000 * 60 * 60 * 24;
                default:
                    throw new ArgumentException("Invalid time unit.");
            }
        }


        private class Task
        {
            public Action TaskMethod { get; }
            public long StartTime { get; }
            public long Interval { get; }
            public int RepeatCount { get; }

            private int currentCount;

            private long lastTick = 0; //上一次执行开始的时间

            public bool Completed = false; //是否已经执行完毕

            public Task(Action taskMethod, long startTime, long interval, int repeatCount)
            {
                TaskMethod = taskMethod;
                StartTime = startTime;
                Interval = interval;
                RepeatCount = repeatCount;
                currentCount = 0;
            }


            //判断本次是否应该执行本任务
            public bool ShouldRun()
            {
                if (currentCount == RepeatCount && RepeatCount != 0)//执行完毕，次数够了
                {
                    Log.Information("RepeatCount={0}", RepeatCount);
                    return false;
                }

                long now = GetCurrentTime();
                if (now >= StartTime && (now - lastTick) >= Interval)
                {
                    return true;
                }

                return false;
            }

            //任务执行
            public void Run()
            {
                lastTick = GetCurrentTime();
                try
                {
                    TaskMethod.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error("Schedule has Error:{0}", ex.Message);
                    //return;
                }


                currentCount++;

                if (currentCount == RepeatCount && RepeatCount != 0)
                {
                    Console.WriteLine("Task completed.");
                    Completed = true;
                }
            }
        }
    }


    public class Time
    {

        private static long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        //游戏运行时间
        public static float time { 
            get {
                return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime) * 0.001f;
            }
            private set { }
        }

        /// <summary>
        /// 获取上一帧运行所用的时间
        /// </summary>
        public static float deltaTime { get; private set; }

        // 记录最后一次tick的时间
        private static long lastTick = 0;

        /// <summary>
        /// 由Schedule调用，请不要自行调用，除非你知道自己在做什么！！！
        /// 更新deltaTime
        /// </summary>
        public static void Tick()
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (lastTick == 0) lastTick = now;
            deltaTime = (now - lastTick) * 0.001f;//deltaTime是以秒作为单位的
            lastTick = now;
        }
    }
}