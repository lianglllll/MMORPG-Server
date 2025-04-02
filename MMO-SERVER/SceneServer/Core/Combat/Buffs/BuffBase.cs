using HS.Protobuf.Combat.Buff;
using SceneServer.Core.Model.Actor;
using SceneServer.Utils;

namespace SceneServer.Core.Combat.Buffs
{
    public abstract class BuffBase
    {
        private bool m_Initialized = false;            

        // 静态字段，可被子类重写
        private string m_Name               = "默认名称";
        private string m_Description        = "这个Buff没有介绍";
        private float m_MaxDuration         = 3;
        private int m_maxLevel              = 1;
        private float m_timeScale           = 1;
        private BuffType m_buffType         = BuffType.Buff;
        private BuffConflict m_buffConflict = BuffConflict.Cover;
        private bool m_dispellable          = true;
        private int m_demotion              = 1;
        private string m_iconPath           = "";

        // 动态字段
        private int m_instanceId            = -1;
        private int m_CurrentLevel          = 0;       
        private float m_buffRemainingTime   = 3;        
        protected BuffDefine m_def;
        private BuffInfo m_buffInfo;

        #region GetSet

        public SceneActor Owner { get; protected set; }
        public SceneActor Provider { get; protected set; }
        public int BID => m_def.BID;
        public string BuffName
        {
            get { return m_Name; }
            protected set { m_Name = value; }
        }
        public string Description
        {
            get { return m_Description; }
            protected set { m_Description = value; }
        }
        public string IconPath
        {
            get { return m_iconPath; }
            protected set { m_iconPath = value; }
        }
        public float MaxDuration
        {
            get { return m_MaxDuration; }
            protected set { m_MaxDuration = Math.Clamp(value, 0, float.MaxValue); }
        }
        public int MaxLevel
        {
            get { return m_maxLevel; }
            protected set { m_maxLevel = Math.Clamp(value, 1, int.MaxValue); }
        }
        public BuffType BuffType
        {
            get { return m_buffType; }
            protected set { m_buffType = value; }
        }
        public BuffConflict BuffConflict
        {
            get { return m_buffConflict; }
            protected set { m_buffConflict = value; }
        }
        public bool Dispellable
        {
            get { return m_dispellable; }
            protected set { m_dispellable = value; }
        }
        public int Demotion
        {
            get { return m_demotion; }
            protected set { m_demotion = Math.Clamp(value, 0, MaxLevel); }
        }
        public float TimeScale
        {
            get { return m_timeScale; }
            set { m_timeScale = Math.Clamp(value, 0, 10); }
        }

        public int InstanceId
        {
            get => m_instanceId;
            set => m_instanceId = value;
        }
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
        public float BuffRemainingTime
        {
            get { return m_buffRemainingTime; }
            set { m_buffRemainingTime = Math.Clamp(value, 0, float.MaxValue); }
        }
        public BuffInfo BuffInfo
        {
            get
            {
                if (m_buffInfo == null)
                {
                    m_buffInfo = new BuffInfo();
                    m_buffInfo.Id = this.InstanceId;
                    m_buffInfo.Bid = this.BID;
                    m_buffInfo.OwnerId = this.Owner.EntityId;
                    m_buffInfo.ProviderId = this.Provider.EntityId;
                }

                m_buffInfo.CurrentLevel = this.CurrentLevel;
                m_buffInfo.ResidualDuration = this.BuffRemainingTime; ;
                return m_buffInfo;
            }
        }

        #endregion

        #region 生命周期函数
        public virtual void Init(SceneActor owner, SceneActor provider, int instanceId)
        {
            if (m_Initialized)
            {
                throw new Exception("不能对已经初始化的buff再次初始化");
            }
            if (owner == null || provider == null)
            {
                throw new Exception("初始化值不能为空");
            }

            m_Initialized = true;
            InstanceId = instanceId;
            Owner = owner;
            Provider = provider;

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
                this.m_def = def;
                this.BuffName = def.Name;
                this.Description = def.Description;
                this.IconPath = def.IconPath;
                this.MaxDuration = def.MaxDuration;
                this.MaxLevel = def.MaxLevel;
                this.Dispellable = def.Dispellable;
                this.Demotion = def.Demotion;
                this.TimeScale = def.TimeScale;
            }
        }
        public virtual void OnGet() {
            // 当Owner获得此buff时触发
        }
        public virtual void OnLost() {
            // 当Owner失去此buff时触发
        }
        public virtual void Update(float delta) {

            // 降低持续时间
            BuffRemainingTime -= delta;

            // 如果持续时间为0，则降级,
            // 降级后如果等级为0则移除，否则刷新持续时间
            if (BuffRemainingTime <= 0)
            {
                CurrentLevel -= Demotion;
                if (CurrentLevel <= 0)
                {
                }
                else
                {
                    BuffRemainingTime = MaxDuration;
                }
            }

        }
        protected virtual void OnLevelChange(int changeLevelDelta) { }
        #endregion

        // tools
        public abstract BuffDefine GetBuffDefine();
    }
}
