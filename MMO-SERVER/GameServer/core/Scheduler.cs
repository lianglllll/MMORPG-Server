using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GameServer.Utils;
using Common.Summer.GameServer;

namespace GameServer
{
    //中心计时器
    public class Scheduler : Singleton<Scheduler>
    {
        private class MyTask
        {
            public Action TaskMethod { get; }
            public long StartTime { get; }
            public long Interval { get; }
            public int RepeatCount { get; }

            private int currentCount;

            private long lastTick = 0; //上一次执行开始的时间

            public bool Completed = false; //是否已经执行完毕

            private bool _isUpdateMethod = false; //是否Update方法


            public MyTask(Action taskMethod, long startTime, long interval, int repeatCount)
            {
                TaskMethod = taskMethod;
                StartTime = startTime;
                Interval = interval;
                RepeatCount = repeatCount;
                currentCount = 0;
            }

            public MyTask(Action taskMethod)
            {
                TaskMethod = taskMethod;
                StartTime = 0;
                Interval = 0;
                RepeatCount = 0;
                currentCount = 0;
                _isUpdateMethod = true; //不限次数，每帧执行
            }


            //判断本次是否应该执行本任务
            public bool ShouldRun()
            {
                if (currentCount == RepeatCount && RepeatCount != 0)//执行完毕，次数够了
                {
                    return false;
                }

                long now = UnixTime;
                if (now >= StartTime && (now - lastTick) >= Interval)
                {
                    return true;
                }

                return false;
            }

            //任务执行
            public void Run()
            {
                lastTick = UnixTime;

                try
                {
                    TaskMethod.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error("Schedule has Error:{0}", ex.Message);
                }

                currentCount++;

                if (currentCount == RepeatCount && RepeatCount != 0)
                {
                    Completed = true;
                }
            }
        }

        private List<MyTask> tasks = new List<MyTask>();                                        // 任务队列
        private ConcurrentQueue<MyTask> _addQueue = new ConcurrentQueue<MyTask>();              // 新增任务队列
        private ConcurrentQueue<Action> _removeQueue = new ConcurrentQueue<Action>();           // 移除任务队列

        private bool _loop = true;
        private Thread loopThread = null;                   //循环线程
        private double _fps;                                // 每秒帧数
        private long _next = 0;                             //下一帧执行的时间
        private int frameCount = 0;                         
        private Stopwatch stopwatch = new Stopwatch();      //用于测量时间间隔


        public Scheduler() { }

        public void Start(bool newThread = true)
        {
            if (loopThread != null) return;
            _loop = true;
            if (newThread)
            {
                loopThread = new Thread(MainLoop);
                loopThread.Start();
            }
            else
            {
                MainLoop();
            }

        }

        public void Stop()
        {
            _loop = false;
        }

        /// <summary>
        /// 给计时器添加一个任务，有一个委托，执行的时间间隔，重复次数（0的话就是不断重复），以秒为间隔
        /// </summary>
        /// <param name="taskMethod"></param>
        /// <param name="intervalSeconds"></param>
        /// <param name="repeatCount"></param>
        public void AddTask(Action taskMethod, float intervalSeconds, int repeatCount = 0)
        {
            this.AddTask(taskMethod, (int)(intervalSeconds * 1000), TimeUnit.Milliseconds, repeatCount);
        }
        public void AddTask(Action taskMethod, int timeValue, TimeUnit timeUnit, int repeatCount = 0)
        {
            int interval = GetInterval(timeValue, timeUnit);
            long startTime = UnixTime + interval;
            MyTask task = new MyTask(taskMethod, startTime, interval, repeatCount);
            _addQueue.Enqueue(task);
        }
        public void AddTask(Action taskMethod, float delay, float interval, int repeatCount = 0)
        {
            int _interval = (int)(interval * 1000);
            long startTime = UnixTime + (long)(delay * 1000);
            MyTask task = new MyTask(taskMethod, startTime, _interval, repeatCount);
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
            _addQueue.Enqueue(new MyTask(action));
        }

        // 计时器主循环
        public void MainLoop()
        {
            stopwatch.Start();
            int frameTime = 1000 / Config.Server.UpdateHz;  //帧间隔时间

            while (_loop)
            {
                var time = UnixTime;
                if (time >= _next)
                {
                    frameCount++;
                    _next = time + frameTime;
                    Execute();

                    //FPS计算逻辑，每秒更新一次FPS
                    if (stopwatch.ElapsedMilliseconds >= 1000)
                    {
                        _fps = frameCount / (stopwatch.ElapsedMilliseconds / 1000.0);
                        frameCount = 0;
                        stopwatch.Restart();
                    }

                }
                //线程切换
                Thread.Sleep(0);
            }

            loopThread = null;
        }

        private void Execute()
        {
            MyTime.Tick();

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
                foreach (MyTask task in tasks)
                {
                    if (task.ShouldRun())
                    {
                        task.Run();
                    }
                }


            }
        }

        //获取从1970年1月1日午夜（也称为UNIX纪元）到现在的毫秒数
        public static long UnixTime { get => DateTimeOffset.Now.ToUnixTimeMilliseconds(); }

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

    }


}