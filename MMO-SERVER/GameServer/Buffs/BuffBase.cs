using GameServer.Model;
using Proto;
using System;

namespace GameServer.Buffs
{
    /// <summary>
    /// Buff的基础类
    /// </summary>
    public abstract class BuffBase
    {
        //静态字段，可被子类重写
        private float m_MaxDuration = 3;
        private float m_TimeScale = 1;
        private int m_MaxLevel = 1;
        private BuffType m_BuffType;
        private BuffConflict m_BuffConflict = BuffConflict.Cover;
        private bool m_Dispellable = true;
        private string m_Name = "默认名称";
        private string m_Description = "这个Buff没有介绍";
        private int m_Demotion = 1;
        private string m_IconPath = "";

        //动态字段
        private int m_CurrentLevel = 0;             //当前等级
        private float m_ResidualDuration = 3;       //当前剩余持续事件
        private bool m_Initialized = false;         //是否初始化

        //网络对象
        private BuffInfo _info;
        public BuffInfo Info
        {
            get
            {
                if(_info == null)
                {
                    _info = new BuffInfo();
                    _info.Id = this.ID;
                    _info.Bid = this.BID;
                    _info.OwnerId = this.Owner.EntityId;
                    _info.ProviderId = this.Provider.EntityId;
                }

                _info.CurrentLevel = this.CurrentLevel;
                _info.ResidualDuration = this.ResidualDuration; ;
                return _info;
            }
        }


        public BuffBase() { }
        /// <summary>
        /// 此buff的持有者
        /// </summary>
        public Actor Owner { get; protected set; }
        /// <summary>
        /// 此Buff提供者
        /// </summary>
        public Actor Provider { get; protected set; }
        /// <summary>
        /// buff的定义
        /// </summary>
        public BuffDefine Def { get; protected set; }
        /// <summary>
        /// buff的编号
        /// </summary>
        public int BID => Def.BID;
        /// <summary>
        /// Buff对外显示的名称
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            protected set { m_Name = value; }
        }
        /// <summary>
        /// Buff的介绍文本
        /// </summary>
        public string Description
        {
            get { return m_Description; }
            protected set { m_Description = value; }
        }
        /// <summary>
        /// Buff的类型，分为正面、负面、中立三种
        /// </summary>
        public BuffType BuffType
        {
            get { return m_BuffType; }
            protected set { m_BuffType = value; }
        }
        /// <summary>
        /// 当两个不同单位向同一个单位施加同一个buff时的冲突处理
        /// 分为三种：
        /// combine,合并为一个buff，叠层（提高等级）
        /// separate,独立存在
        /// cover, 覆盖，后者覆盖前者
        /// </summary>
        public BuffConflict BuffConflict
        {
            get { return m_BuffConflict; }
            protected set { m_BuffConflict = value; }
        }
        /// <summary>
        /// Buff的初始持续时间
        /// </summary>
        public float MaxDuration
        {
            get { return m_MaxDuration; }
            protected set { m_MaxDuration = Math.Clamp(value, 0, float.MaxValue); }
        }
        /// <summary>
        /// buff的时间流失速度，最小为0，最大为10。
        /// </summary>
        public float TimeScale
        {
            get { return m_TimeScale; }
            set { m_TimeScale = Math.Clamp(value, 0, 10); }
        }
        /// <summary>
        /// buff的最大堆叠层数，最小为1，最大为2147483647
        /// </summary>
        public int MaxLevel
        {
            get { return m_MaxLevel; }
            protected set { m_MaxLevel = Math.Clamp(value, 1, int.MaxValue); }
        }
        /// <summary>
        /// Buff的当前等级
        /// </summary>
        public int CurrentLevel
        {
            get { return m_CurrentLevel; }
            set
            {
                //计算出改变值
                int change = Math.Clamp(value, 0, MaxLevel) - m_CurrentLevel;
                OnLevelChange(change);
                m_CurrentLevel += change;
            }
        }
        /// <summary>
        /// 每次Buff持续时间结束时降低的等级，一般降低1级或者降低为0级。
        /// </summary>
        public int Demotion
        {
            get { return m_Demotion; }
            protected set { m_Demotion = Math.Clamp(value, 0, MaxLevel); }
        }
        /// <summary>
        /// 可否被驱散
        /// </summary>
        public bool Dispellable
        {
            get { return m_Dispellable; }
            protected set { m_Dispellable = value; }
        }
        /// <summary>
        /// 图标资源的路径
        /// </summary>
        public string IconPath
        {
            get { return m_IconPath; }
            protected set { m_IconPath = value; }
        }


        /// <summary>
        /// Buff的当前剩余时间
        /// </summary>
        public float ResidualDuration
        {
            get { return m_ResidualDuration; }
            set { m_ResidualDuration = Math.Clamp(value, 0, float.MaxValue); }
        }
        /// <summary>
        /// 实例ID,因为同一个buff可能有很多个，需要区分
        /// </summary>
        public int ID { get; set; }


        /// <summary>
        /// 获取buff定义
        /// </summary>
        /// <returns></returns>
        public abstract BuffDefine GetBuffDefine();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="provider"></param>
        /// <exception cref="Exception"></exception>
        public virtual void Init(Actor owner, Actor provider)
        {
            if (m_Initialized)
            {
                throw new Exception("不能对已经初始化的buff再次初始化");
            }
            if (owner == null || provider == null)
            {
                throw new Exception("初始化值不能为空");
            }
            Owner = owner;
            Provider = provider;
            m_Initialized = true;

            var def = GetBuffDefine();
            if (def != null)
            {
                this.BuffType = def.BuffType switch
                {
                    "正增益" => BuffType.Buff,
                    "负增益" => BuffType.Debuff,
                    _ => BuffType.Buff,
                };
                this.BuffConflict = def.BuffConflict switch
                {
                    "合并" => BuffConflict.Combine,
                    "独立" => BuffConflict.Separate,
                    "覆盖" => BuffConflict.Cover,
                    _ => throw new Exception("Buff BuffConflict Not Found ：" + def.BuffConflict),
                };
                this.Def = def;
                this.Name = def.Name;
                this.Description = def.Description;
                this.IconPath = def.IconPath;
                this.MaxDuration = def.MaxDuration;
                this.MaxLevel = def.MaxLevel;
                this.Dispellable = def.Dispellable;
                this.Demotion = def.Demotion;
                this.TimeScale = def.TimeScale;
            }
        }

        /// <summary>
        /// 当Owner获得此buff时触发
        /// 由BuffManager在合适的时候调用
        /// </summary>
        public virtual void OnGet() { }

        /// <summary>
        /// 当Owner失去此buff时触发
        /// 由BuffManager在合适的时候调用
        /// </summary>
        public virtual void OnLost() { }

        /// <summary>
        /// Update,由BuffManager每物理帧调用
        /// </summary>
        public virtual void OnUpdate(float delta) { }

        /// <summary>
        /// 当等级改变时调用
        /// </summary>
        /// <param name="change">改变了多少级</param>
        protected virtual void OnLevelChange(int change) { }

    }
}
