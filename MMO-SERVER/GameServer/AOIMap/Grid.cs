
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameServer.Model;

namespace AOIMap
{
    /// <summary>
    /// 格子
    /// </summary>
    public class Grid
    {
        public int GID { get; private set; }
        private HashSet<IAOIUnit> aoiSet;           //我们需要同步的单位：角色、怪物、npc、物品
        private ReaderWriterLockSlim pIDLock;       //读写锁

        public Grid(int id)
        {
            GID = id;
            aoiSet = new();
            pIDLock = new ();
        }

        /// <summary>
        /// 添加需要同步的单位
        /// </summary>
        /// <param name="obj"></param>
        public void Add(IAOIUnit obj)
        {
            pIDLock.EnterReadLock();
            try
            {
                if (!aoiSet.Contains(obj))
                {
                    aoiSet.Add(obj);
                }
            }
            finally
            {
                pIDLock.ExitReadLock();
            }
            
        }

        /// <summary>
        /// 移除需要同步的单位
        /// </summary>
        /// <param name="obj"></param>
        public void Remove(IAOIUnit obj)
        {
            pIDLock.EnterReadLock();
            try
            {
                aoiSet.Remove(obj);
            }
            finally
            {
                pIDLock.ExitReadLock();
            }
            
        }

        /// <summary>
        /// 获取当前格子内全部的同步单位
        /// </summary>
        /// <returns></returns>
        public List<IAOIUnit> GetEntities()
        {
            pIDLock.EnterReadLock();
            try
            {
                return aoiSet.ToList();
            }
            finally
            {
                pIDLock.ExitReadLock();
            }
        }

    }
}
