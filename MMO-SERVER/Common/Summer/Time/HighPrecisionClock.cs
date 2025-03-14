using System;
using System.Diagnostics;

namespace Common.Summer.Time
{
    public class HighPrecisionClock
    {
        private readonly DateTimeOffset _baseTime;
        private readonly long _baseTimestamp;
        private readonly double _tickToMilliFactor;  // 这是每个stopwatch刻度的时间，单位是毫秒

        public HighPrecisionClock()
        {
            _baseTime = DateTimeOffset.UtcNow;
            _baseTimestamp = Stopwatch.GetTimestamp();
            _tickToMilliFactor = 1000.0 / Stopwatch.Frequency; // == (1 / Stopwatch.Frequency) * 1000
        }

        /// <summary>
        /// 获取当前UTC时间的Unix时间戳（毫秒级）
        /// </summary>
        public long GetUnixTimeMilliseconds()
        {
            // 手动计算流逝时间
            long currentTicks = Stopwatch.GetTimestamp();
            long elapsedTicks = currentTicks - _baseTimestamp;
            TimeSpan elapsed = new TimeSpan((long)(elapsedTicks * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));

            DateTimeOffset currentTime = _baseTime.Add(elapsed);
            return currentTime.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 获取程序启动后的流逝时间（毫秒）
        /// </summary>
        public long GetElapsedMilliseconds()
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - _baseTimestamp;
            return (long)(elapsedTicks * _tickToMilliFactor);
        }
    }
}