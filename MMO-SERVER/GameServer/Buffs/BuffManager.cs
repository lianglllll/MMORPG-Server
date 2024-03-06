using Common.Summer;
using GameServer.Model;
using Proto;
using Summer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Buffs
{
    /// <summary>
    /// 每个角色都有自己的Buff管理器
    /// 更新buff的逻辑
    /// </summary>
    public class BuffManager
    {
        //用于分配全局唯一的id
        private static IdGenerator _idGenerator = new IdGenerator();

        public Actor Owner { get; protected set; }
        public List<BuffBase> buffs { get; protected set; }
        public Action<BuffBase> Observer;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BuffManager() { }
        public BuffManager(Actor Owner)
        {
            this.Owner = Owner;
            buffs = new();
        }

        /// <summary>
        /// 添加一个buff
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider">提供者</param>
        /// <param name="level"></param>
        public void AddBuff<T>(Actor provider, int level = 1) where T : BuffBase, new()
        {

            //遍历看看目标身上有没有已存在的要挂的buff。
            List<T> temp01 = buffs.OfType<T>().ToList();

            //如果没有直接挂一个新buff就行了
            //如果有已存在的要挂的buff，就要进行冲突处理了
            if (temp01.Count == 0)
            {
                AddNewBuff<T>(provider, level);
            }
            else
            {
                switch (temp01[0].BuffConflict)
                {
                    //如果是独立存在，那也直接挂buff
                    case BuffConflict.Separate:
                        bool temp = true;
                        foreach (T item in temp01)
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
                            AddNewBuff<T>(provider, level);
                        }
                        break;
                    //如果是合并，则跟已有的buff叠层。
                    case BuffConflict.Combine:
                        temp01[0].CurrentLevel += level;
                        break;
                    //如果是覆盖，则移除旧buff，然后添加这个buff。
                    case BuffConflict.Cover:
                        RemoveBuff(temp01[0]);
                        AddNewBuff<T>(provider, level);
                        break;
                }
            }

        }
        private void AddNewBuff<T>(Actor provider, int level) where T : BuffBase, new()
        {
            T buff = new T();
            buff.ID = _idGenerator.GetId();
            buff.Init(Owner, provider);
            buffs.Add(buff);
            buff.ResidualDuration = buff.MaxDuration;
            buff.CurrentLevel = level;
            buff.OnGet();
            Observer?.Invoke(buff);

            //通知客户端
            var resp = new BuffsAddResponse();
            resp.List.Add(buff.Info);
            Owner?.currentSpace?.Broadcast(resp);

        }

        /// <summary>
        /// 移除单位身上指定的一个buff
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="buff"></param>
        /// <returns>是否成功，如果失败说明目标不存在</returns>
        public bool RemoveBuff(BuffBase buff)
        {
            var item = buffs.FirstOrDefault(x => x == buff);
            if (item != null)
            {
                item.CurrentLevel -= item.CurrentLevel;
                item.OnLost();
                buffs.Remove(item);
                _idGenerator.ReturnId(item.ID);

                //通知客户端
                var resp = new BuffsRemoveResponse();
                resp.List.Add(item.Info);
                Owner?.currentSpace?.Broadcast(resp);

                return true;
            }
            return false;
        }

        /// <summary>
        /// 获得单位身上指定类型的buff的列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Owner"></param>
        /// <returns></returns>
        public List<T> FindBuff<T>() where T : BuffBase, new()
        {
            List<T> result = buffs.OfType<T>().ToList();
            return result;
        }

        /// <summary>
        /// 推动buff运行
        /// </summary>
        /// <param name="delta"></param>
        public void OnUpdate(float delta)
        {
            //所有Buff执行Update
            foreach (BuffBase item in buffs)
            {
                if (item.CurrentLevel > 0 && item.Owner != null)
                {
                    item.OnUpdate(delta);
                }
            }

            //降低持续时间,清理无用buff
            //这里从尾部开始，避免破坏list顺序
            for (int i = buffs.Count - 1; i >= 0; i--)
            {
                var buff = buffs[i];
                //如果等级为0，则移除
                if (buff.CurrentLevel == 0)
                {
                    RemoveBuff(buff);
                    continue;
                }
                //如果持续时间为0，则降级,
                //降级后如果等级为0则移除，否则刷新持续时间
                if (buff.ResidualDuration <= 0)
                {
                    buff.CurrentLevel -= buff.Demotion;
                    if (buff.CurrentLevel <= 0)
                    {
                        RemoveBuff(buff);
                        continue;
                    }
                    else
                    {
                        buff.ResidualDuration = buff.MaxDuration;
                    }
                }
                //降低持续时间
                buff.ResidualDuration -= delta;
            }

        }

    }
}
