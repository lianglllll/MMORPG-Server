using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Tools;
using HS.Protobuf.MasterTime;
using Serilog.Core;
using System.Diagnostics;

namespace MasterTimerServer.Core
{
    public class PrecisionTimeService : Singleton<PrecisionTimeService>
    {
        // 自定义纪元（解决2038年问题）
        private static readonly DateTime _epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 使用更精确的初始时间基准
        private readonly DateTime _initialUtc;
        private readonly long _initialStopwatchTicks;
        private readonly double _stopwatchTickToMilliseconds;

        // 线程同步锁
        private readonly object _syncLock = new object();

        public PrecisionTimeService()
        {
            // 初始化时间基准（确保原子性操作）
            _initialUtc = DateTime.UtcNow;
            _initialStopwatchTicks = Stopwatch.GetTimestamp();
            _stopwatchTickToMilliseconds = 1000.0 / Stopwatch.Frequency;

            // 验证高精度计时器
            if (!Stopwatch.IsHighResolution)
            {
                throw new NotSupportedException("System does not support high-resolution timing");
            }
        }
        public bool Init()
        {
            // 协议注册
            ProtoHelper.Instance.Register<TimeSyncRequest>((int)MasterTimeProtocl.TimeSyncReq);
            ProtoHelper.Instance.Register<TimeSyncResponse>((int)MasterTimeProtocl.TimeSyncResp);

            // 消息订阅
            MessageRouter.Instance.Subscribe<TimeSyncRequest>(_HandleTimeSyncRequest);

            return true;
        }

        // 获取当前精确时间（线程安全）
        private (long ticks, double time) GetPrecisionTime()
        {
            lock (_syncLock)
            {
                long currentTicks = Stopwatch.GetTimestamp();
                return (currentTicks, CalculateTime(currentTicks));
            }
        }

        // 优化后的时间计算方法
        private double CalculateTime(long currentStopwatchTicks)
        {
            // 计算经过的Stopwatch ticks
            long elapsedTicks = currentStopwatchTicks - _initialStopwatchTicks;

            // 计算总时间（基准时间 + 高精度偏移）
            double elapsedMilliseconds = elapsedTicks * _stopwatchTickToMilliseconds;
            return (_initialUtc - _epoch).TotalMilliseconds + elapsedMilliseconds;
        }

        private void _HandleTimeSyncRequest(Connection conn, TimeSyncRequest message)
        {

        }
    }
}