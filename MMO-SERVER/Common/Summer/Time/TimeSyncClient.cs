using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Time;
using Common.Summer.Tools;
using HS.Protobuf.MasterTime;
using Serilog;
using Serilog.Core;

namespace Client.TimeSync
{
    public class TimeSyncClient : Singleton<TimeSyncClient>
    {
        private NetClient m_timerServer;

        private readonly HighPrecisionClock _clock = new HighPrecisionClock();
        private long m_timeOffset;
        private readonly object m_offsetLock = new object();

        // 同步控制参数
        private int m_syncInterval = 5000;           // 基础同步间隔（毫秒）
        private int m_fastSyncDuration = 30000;      // 快速同步阶段时长
        private int m_minSyncInterval = 1000;        // 最小同步间隔
        private int m_maxSyncInterval = 60000;       // 最大同步间隔
        private readonly object m_syncIntervalLock = new object();
        private Timer m_syncTimer;
        private Stopwatch m_operationTime = new Stopwatch();
        private Queue<long> m_offsetHistory = new Queue<long>(10);

        public void Init(NetClient netClient)
        {
            m_timerServer = netClient;

            ProtoHelper.Instance.Register<TimeSyncRequest>((int)MasterTimeProtocl.TimeSyncReq);
            ProtoHelper.Instance.Register<TimeSyncResponse>((int)MasterTimeProtocl.TimeSyncResp);
            MessageRouter.Instance.Subscribe<TimeSyncResponse>(HandleTimeSyncResponse);

            // 启动同步计时器
            m_syncTimer = new Timer(SyncCallback, null, 0, Timeout.Infinite);
            m_operationTime.Start();
        }
        private void SyncCallback(object state)
        {
            try
            {
                SendTimeSyncRequest();

                // 动态调整间隔策略
                var elapsed = m_operationTime.ElapsedMilliseconds;
                if (elapsed < m_fastSyncDuration)
                {
                    // 快速同步阶段：前30秒每1秒同步
                    m_syncInterval = Math.Max(m_minSyncInterval, 1000);
                }
                else
                {
                    // 稳定阶段：根据偏移量动态调整
                    lock (m_offsetLock)
                    {
                        var absOffset = Math.Abs(m_timeOffset);
                        m_syncInterval = absOffset switch
                        {
                            > 50 => 2000,    // 大偏差时2秒同步
                            > 20 => 5000,    // 中等偏差5秒
                            > 10 => 10000,   // 小偏差10秒
                            _ => 30000       // 正常状态30秒
                        };
                    }
                }

                // 限制间隔范围
                m_syncInterval = Math.Clamp(m_syncInterval, m_minSyncInterval, m_maxSyncInterval);
            }
            catch
            {
                HandleNetworkError();
            }
            finally
            {
                // 重新设置定时器
                m_syncTimer?.Change(m_syncInterval, Timeout.Infinite);
            }
        }
        private void HandleNetworkError()
        {
            // 遇到错误时缩短同步间隔
            m_syncInterval = Math.Max(m_minSyncInterval, m_syncInterval / 2);
            Log.Warning($"Network error detected, adjusting sync interval to {m_syncInterval}ms");
        }
        public void Shutdown()
        {
            m_syncTimer?.Dispose();
            m_operationTime.Stop();
        }
        public void SendTimeSyncRequest()
        {
            var req = new TimeSyncRequest();
            req.ClientSendTime = _clock.GetUnixTimeMilliseconds();
            m_timerServer.Send(req);
        }
        private void HandleTimeSyncResponse(Connection conn, TimeSyncResponse message)
        {
            long t1 = message.ClientSendTime;
            long t2 = message.ServerReceiveTime;
            long t3 = message.ServerSendTime;
            long t4 = _clock.GetUnixTimeMilliseconds();

            // 计算时延和偏移量
            long roundTripTime = (t4 - t1) - (t3 - t2);
            long clockOffset = ((t2 - t1) + (t3 - t4)) / 2;   // 实际上是t3 + roundTripTime/2 = t4 + offset

            lock (m_offsetLock)
            {
                // 使用加权平均平滑偏移量
                double newOffset = m_timeOffset * 0.7 + clockOffset * 0.3;
                m_timeOffset = (long)Math.Round(newOffset);
            }

            // 记录历史偏移量
            lock (m_offsetLock)
            {
                if (m_offsetHistory.Count >= 10)
                {
                    m_offsetHistory.Dequeue();
                }
                m_offsetHistory.Enqueue(m_timeOffset);
            }

            // 计算偏移量标准差
            var stdDev = CalculateOffsetDeviation();
            if (stdDev > 15)
            {
                m_syncInterval = Math.Max(m_minSyncInterval, m_syncInterval - 1000);
            }

        }
        private double CalculateOffsetDeviation()
        {
            lock (m_offsetLock)
            {
                if (m_offsetHistory.Count < 2) return 0; // 至少需要2个数据点

                // 单次遍历计算方差
                double sum = 0;
                double sumSq = 0;
                int count = 0;

                foreach (var offset in m_offsetHistory)
                {
                    sum += offset;
                    sumSq += offset * (double)offset;
                    count++;
                }

                double variance = (sumSq - (sum * sum) / count) / count;
                return Math.Sqrt(variance);
            }
        }

        // 对外提供的工具
        public long GetSyncedUnixTimeMilliseconds()
        {
            lock (m_offsetLock)
            {
                // 直接返回同步后的时间戳
                return _clock.GetUnixTimeMilliseconds() + m_timeOffset;
            }
        }
        public DateTimeOffset GetSynchronizedUtcTime()
        {
            long baseTime;
            long offset;

            lock (m_offsetLock)
            {
                // 双读取确保原子性
                baseTime = _clock.GetUnixTimeMilliseconds();
                offset = m_timeOffset;
            }

            // 带有时区信息的精确时间
            return DateTimeOffset.FromUnixTimeMilliseconds(baseTime + offset);
        }
    }
}