using Common.Summer.Core;
using Common.Summer.Net;
using Common.Summer.Proto;
using Common.Summer.Tools;
using HS.Protobuf.Common;
using LoginGateServer.Net;
using Serilog;
using System.Collections.Concurrent;

namespace LoginGateServer.Core
{
    /*
    public class ClientMessageDispatcher : Singleton<ClientMessageDispatcher>
    {
        ConcurrentDictionary<int, ConcurrentDictionary<int, TaskCompletionSource<TCPEnvelope>>> m_task = new();

        // 转发 
        public void Init()
        {
            ProtoHelper.Register<IPEnvelope>((int)CommonProtocl.IpEnvelope);
            ProtoHelper.Register<TCPEnvelope>((int)CommonProtocl.TcpEnvelope);
            MessageRouter.Instance.Subscribe<IPEnvelope>(_HandleIPEnvelope);
            MessageRouter.Instance.Subscribe<TCPEnvelope>(_HandleTCPEnvelope);
        }
        public void UnInit()
        {

        }

        private async void _HandleIPEnvelope(Connection conn, IPEnvelope message)
        {
            try
            {
                // 加解密

                // 传递
                if (message.ProtocolCode == (int)PROTOCAL_CODE.Login)
                {
                    message.TcpEnvelope = await _DispatchMessageToLogin(message.TcpEnvelope);
                    conn.Send(message);
                }
            }
            catch(Exception e)
            {
                Log.Error(e, "HandleIPEnvelope error");
            }

        }
        private void _HandleTCPEnvelope(Connection sender, TCPEnvelope message)
        {
            if (m_task.TryGetValue(message.ClientId, out var clientDict) &&
                clientDict.TryRemove(message.SeqId, out var tcs))
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(message);
                }
                else
                {
                    // Handle the situation where the task is already completed, if necessary.
                    // For example, log a warning or handle reuse case.
                }
            }
        }
        private async Task<TCPEnvelope> _DispatchMessageToLogin(TCPEnvelope tcpEnvelope)
        {
            var clientDict = m_task.GetOrAdd(tcpEnvelope.ClientId, _ => new ConcurrentDictionary<int, TaskCompletionSource<TCPEnvelope>>());
            var tcs = new TaskCompletionSource<TCPEnvelope>();

            // 如果存在相同 PortId 的任务, 可以选择抛出异常或者覆盖现有任务
            if (!clientDict.TryAdd(tcpEnvelope.SeqId, tcs))
            {
                throw new InvalidOperationException("A task with the same PortId already exists.");
            }

            try
            {
                ServersMgr.Instance.SentToLoginServer(tcpEnvelope);

                Console.WriteLine("2 thread ID: " + Thread.CurrentThread.ManagedThreadId);

                // 设置超时机制，避免长时间等待
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));
                Console.WriteLine("3 thread ID: " + Thread.CurrentThread.ManagedThreadId);

                if (completedTask != tcs.Task)
                {
                    tcs.TrySetCanceled();
                    throw new TimeoutException("The operation timed out.");
                }

                return await tcs.Task; // 返回处理后的结果
            }
            finally
            {
                Console.WriteLine("4 thread ID: " + Thread.CurrentThread.ManagedThreadId);

                // 确保在任务完成后移除条目，防止内存泄漏
                clientDict.TryRemove(tcpEnvelope.SeqId, out _);
                if (clientDict.IsEmpty)
                {
                    m_task.TryRemove(tcpEnvelope.ClientId, out _);
                }
            }
        }
    }
    */
}
