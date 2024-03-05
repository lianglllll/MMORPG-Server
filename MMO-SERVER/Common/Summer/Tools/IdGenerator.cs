using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Summer
{
    /// <summary>
    /// id生成器
    /// </summary>
    public class IdGenerator
    {
        private int _currentMaxId; // 用于记录当前分配到的最大ID
        private ConcurrentBag<int> _recycledIds; // 存储回收的ID,线程安全

        public IdGenerator()
        {
            _currentMaxId = 0;
            _recycledIds = new ConcurrentBag<int>();
        }

        // 获取新的ID
        public int GetId()
        {
            // 首先尝试从_recycledIds中获取一个可重复利用的ID
            if (_recycledIds.TryTake(out int recycledId))
            {
                return recycledId;
            }

            // 如果没有可重复利用的ID，则生成一个新的ID
            // 使用Interlocked.Increment来确保操作的原子性，防止多线程下的数据竞争问题
            return Interlocked.Increment(ref _currentMaxId);
        }

        // 归还不再使用的ID，以便将来重复利用
        public void ReturnId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("ID must be greater than 0.");
            }

            _recycledIds.Add(id);
        }
    }
}
