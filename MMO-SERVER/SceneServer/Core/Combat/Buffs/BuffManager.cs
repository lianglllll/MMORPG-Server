using Common.Summer.Tools;
using HS.Protobuf.Combat.Buff;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene;

namespace SceneServer.Core.Combat.Buffs
{
    // 每个角色都有自己的Buff管理器
    public class BuffManager
    {
        private static IdGenerator  m_idGenerator = new();
        private SceneActor          m_owner;
        public Action<BuffBase>     m_observer;
        private Dictionary<int, List<BuffBase>> m_buffMap;
        private Queue<BuffBase> removeQueue;

        #region GetSet

        public SceneActor Owner {
            get => m_owner; 
            protected set => m_owner = value;
        }

        #endregion

        #region 生命周期函数
        
        public bool Init(SceneActor Owner)
        {
            m_owner = Owner;
            m_buffMap = new();
            removeQueue = new Queue<BuffBase>();
            return true;
        }
        public void Update(float delta)
        {
            // 所有Buff执行Update
            foreach (var kv in m_buffMap)
            {
                foreach(var item in kv.Value)
                {
                    if (item.CurrentLevel > 0 && item.Owner != null)
                    {
                        item.Update(delta);
                    }
                }
            }

            // 移除过期buff
            foreach (var buff in removeQueue)
            {
                RemoveBuff(buff);
            }
        }

        #endregion

        public void AddBuff(SceneActor provider, int buffId, int level = 1)
        {
            // 遍历看看目标身上有没有已存在的要挂的buff。
            bool isHaveBuff = m_buffMap.TryGetValue(buffId, out var buffBases);

            if(!isHaveBuff)
            {
                // 如果没有直接挂一个新buff就行了
                AddNewBuff(provider, buffId, level);
            }
            else
            {
                // 如果有已存在的要挂的buff，就要进行冲突处理了
                switch (buffBases[0].BuffConflict)
                {
                    // 如果是独立存在，那也直接挂buff
                    case BuffConflict.Separate:
                        bool temp = true;
                        foreach (var item in buffBases)
                        {
                            if (item.Provider == provider)
                            {
                                item.CurrentLevel += level;
                                temp = false;
                                continue;
                            }
                        }
                        if (temp)
                        {
                            AddNewBuff(provider, buffId, level);
                        }
                        break;
                    // 如果是合并，则跟已有的buff叠层。
                    case BuffConflict.Combine:
                        buffBases[0].CurrentLevel += level;
                        break;
                    // 如果是覆盖，则移除旧buff，然后添加这个buff。
                    case BuffConflict.Cover:
                        RemoveBuff(buffBases[0]);
                        AddNewBuff(provider, buffId, level);
                        break;
                }
            }


        }
        private void AddNewBuff(SceneActor provider, int buffId, int level)
        {
            var buff = BuffScanner.CreateBuff(buffId);
            buff.Init(Owner, provider, m_idGenerator.GetId());
            if(m_buffMap.TryGetValue(buffId, out var list))
            {
                list.Add(buff);
            }
            else
            {
                m_buffMap.Add(buffId,new List<BuffBase> { buff});
            }
            buff.BuffRemainingTime = buff.MaxDuration;
            buff.CurrentLevel = level;
            buff.OnGet();
            // m_observer?.Invoke(buff);
            BuffsChangePostProcessing();

            // 广播通知客户端
            var resp = new BuffOperationResponse();
            resp.OperationType = BuffOperationType.BuffOperationAdd;
            resp.BuffInfo = buff.BuffInfo;
            SceneManager.Instance.Broadcast(Owner.EntityId, true, resp);
        }
        public void AddReadyRemoveQueue(BuffBase buff)
        {
            removeQueue.Enqueue(buff);
        }
        public bool RemoveBuff(BuffBase buff)
        {
            //Log.Information("移除的buff" + buff.Name);
            int buffId = buff.BID;
            m_buffMap.TryGetValue(buffId, out var list);
            BuffBase targetBuff = null;
            foreach(var item in list)
            {
                if(item == buff)
                {
                    targetBuff = item;
                    break;
                }
            }
            if(targetBuff != null)
            {
                targetBuff.CurrentLevel -= targetBuff.CurrentLevel;
                targetBuff.OnLost();
                list.Remove(targetBuff);
                m_idGenerator.ReturnId(targetBuff.InstanceId);
                BuffsChangePostProcessing();
                // 广播通知客户端
                var resp = new BuffOperationResponse();
                resp.OperationType = BuffOperationType.BuffOperationRemove;
                resp.BuffInfo = buff.BuffInfo;
                SceneManager.Instance.Broadcast(Owner.EntityId, true, resp);
                return true;
            }

            return false;
        }
        public void RemoveAllBuff()
        {

            foreach (var kv in m_buffMap)
            {
                foreach (var item in kv.Value)
                {
                    item.OnLost();
                    m_idGenerator.ReturnId(item.InstanceId);
                }
            }
            m_buffMap.Clear();

            // 告诉其他人
            // Owner?.currentSpace?.AOIBroadcast(Owner, resp, true);
        }



        private void BuffsChangePostProcessing()
        {
            //更新actor网络对象上面的buff信息
            var netNodeBuffs = Owner.NetActorNode.Buffs;
            netNodeBuffs.Clear();

            foreach (var kv in m_buffMap)
            {
                foreach (var item in kv.Value)
                {
                    netNodeBuffs.Add(item.BuffInfo);
                }
            }

        }
    }
}
