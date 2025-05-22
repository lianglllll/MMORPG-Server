using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.Attrubute;
using SceneServer.Utils;
using SceneServer.Core.Combat.Skills;
using HS.Protobuf.Common;
using SceneServer.Core.Scene;
using SceneServer.Core.Combat.Buffs;
using HS.Protobuf.Scene;
using Google.Protobuf.Collections;

namespace SceneServer.Core.Model.Actor
{
    public class SceneActor : SceneEntity
    {
        private bool m_isInit = false;

        public UnitDefine? m_define;
        protected NetActorNode m_netActorNode = new();

        // attribute
        protected AttributeManager m_attributeManager = new();

        // skill
        protected Skill         m_curUseSkill;
        protected SkillSpell    m_skillSpell = new();
        protected SkillManager  m_skillManager = new();

        // buff
        public BuffManager m_buffManager = new();
        private bool isCanReciveDamage;

        #region GetSet
        public NetActorNode NetActorNode
        {
            get
            {
                return m_netActorNode;
            }
            private set { }
        }
        public AttrubuteData CurAttrubuteDate => m_attributeManager.final;
        public NetActorState NetActorState => m_netActorNode.NetActorState;
        public bool IsDeath => m_netActorNode.NetActorState == NetActorState.Death;
        public int CurHP
        {
            get => m_netActorNode.Hp;
            set => m_netActorNode.Hp = value;
        }
        public int CurMP
        {
            get => m_netActorNode.Mp;
            set => m_netActorNode.Mp = value;
        }
        public int MaxHP => m_netActorNode.MaxHp;
        public int MaxMP => m_netActorNode.MaxMp;
        public int Speed
        {
            get => m_netActorNode.Speed;
            set
            {
                m_netActorNode.Speed = value;
            }
        }
        public int CurLevel
        {
            get => m_netActorNode.Level;
            set => m_netActorNode.Level = value;
        }
        public int CurSceneId
        {
            get
            {
                return m_netActorNode.SceneId;
            }
            set
            {
                m_netActorNode.SceneId = value;
            }
        }
        public Skill CurUseSkill
        {
            get
            {
                return m_curUseSkill;
            }
            set
            {
                m_curUseSkill = value;
            }
        }
        public Skill GetSkillById(int skillId)
        {
            return m_skillManager.GetSkillById(skillId);
        }
        public SkillSpell SkillSpell => m_skillSpell;
        public void SetActorReciveDamageMode(bool active)
        {
            isCanReciveDamage = active;
        }
        public AttributeManager AttributeManager => m_attributeManager;
        #endregion

        #region 生命周期
        public void Init(NetVector3 InitPos, int professionId, int level, RepeatedField<NetEquipmentNode> equips)
        {
            base.Init(InitPos, Vector3Int.zero, Vector3Int.one);

            m_define = StaticDataManager.Instance.unitDefineDict[professionId];
            m_attributeManager.Init(m_define, level, equips);
            CurUseSkill = null;
            m_skillSpell.Init(this);
            m_skillManager.Init(this);
            m_buffManager.Init(this);

            var transform = new NetTransform();
            var pos = new NetVector3();
            var rotation = new NetVector3();
            var scale = new NetVector3();
            transform.Position = pos;
            transform.Rotation = rotation;
            transform.Scale = scale;
            m_netActorNode.Transform = transform;
            m_netActorNode.Transform.Position = Position;
            m_netActorNode.Transform.Rotation = Rotation;
            m_netActorNode.Transform.Scale = Scale;

            m_netActorNode.ActorName = m_define.Name;
            m_netActorNode.ProfessionId = professionId;
            m_netActorNode.Level = level;
            m_netActorNode.MaxHp = m_attributeManager.final.MaxHP;
            m_netActorNode.MaxMp = m_attributeManager.final.MaxMP;
            m_netActorNode.Hp = MaxHP;
            m_netActorNode.Mp = MaxMP;
            m_netActorNode.Speed = m_attributeManager.final.Speed;
            m_netActorNode.NetActorMode = NetActorMode.Normal;
            m_netActorNode.NetActorState = NetActorState.Idle;
            m_netActorNode.SceneId = SceneManager.Instance.SceneId;

            isCanReciveDamage = true;
            m_isInit = true;
        }
        public override void Update(float deltaTime)
        {
            if (!m_isInit) return;
            m_skillManager.Update(deltaTime);
            m_buffManager.Update(deltaTime);
        }
        #endregion

        #region 属性变更
        public override void SetTransform(NetTransform transform)
        {
            base.SetTransform(transform);
            // 同时存储在netActorNode中
            m_netActorNode.Transform.Position = transform.Position;
            m_netActorNode.Transform.Rotation = transform.Rotation;
            m_netActorNode.Transform.Scale = transform.Scale;
        }
        public bool ChangeActorState(NetActorState state)
        {
            m_netActorNode.NetActorState = state;

            // 中断当前执行的技能
            if(m_netActorNode.NetActorState != NetActorState.Skill)
            {
                _CancelCurSkill();
            }

            return true;
        }
        public bool ChangeActorMode(NetActorMode mode)
        {
            m_netActorNode.NetActorMode = mode;
            _CancelCurSkill();
            return true;
        }
        public bool ChangeHP(int hpDelta)
        {
            bool result = false;

            if (hpDelta == 0)
            {
                goto End;
            }

            int oldValue = CurHP;
            CurHP += hpDelta;

            if (CurHP <= 0)
            {
                CurHP = 0;
            }
            if (CurHP > MaxHP)
            {
                CurHP = MaxHP;
            }

            // 发包
            ActorPropertyUpdate po = new()
            {
                EntityId = EntityId,
                PropertyType = PropertyType.Hp,
                OldValue = new() { IntValue = oldValue },
                NewValue = new() { IntValue = CurHP },
            };
            SceneManager.Instance.FightManager.propertyUpdateQueue.Enqueue(po);

        End:
            return result;
        }
        public bool ChangeMP(int mpDelta)
        {
            bool result = false;

            if (mpDelta == 0)
            {
                goto End;
            }

            int oldValue = CurMP;
            CurMP += mpDelta;

            if (CurMP <= 0)
            {
                CurMP = 0;
            }
            if (CurMP > MaxMP)
            {
                CurMP = MaxMP;
            }

            // 发包
            ActorPropertyUpdate po = new()
            {
                EntityId = EntityId,
                PropertyType = PropertyType.Mp,
                OldValue = new() { IntValue = oldValue },
                NewValue = new() { IntValue = CurMP },
            };
            SceneManager.Instance.FightManager.propertyUpdateQueue.Enqueue(po);

        End:
            return result;
        }
        public void RecvDamage(Damage damage)
        {
            if (IsDeath) return;

            // 由技能和buff触出，当前actor收到扣血通知
            // 扣血，属性更新
            if (CurHP > damage.Amount)
            {
                ChangeHP(-(int)damage.Amount);
            }
            else
            {
                ChangeHP(-CurHP);
                Death(damage.AttackerId);
            }

            // 添加广播，一个伤害发生了。
            SceneManager.Instance.FightManager.damageQueue.Enqueue(damage);

            RecvDamageAfter(damage);
        }
        protected virtual void RecvDamageAfter(Damage damage) { }
        protected virtual void Death(int killerID) { }
        protected virtual void DeathAfter(int killerID) { }
        public virtual void Revive()
        {

        }
        protected virtual void ReviveAfter() { }
        #endregion

        #region tools
        private void _CancelCurSkill()
        {
            if(CurUseSkill == null)
            {
                goto End;
            }
            CurUseSkill.CancelSkill();
            CurUseSkill = null;

        End:
            return;
        }
        protected virtual void ForceChangeSelfActor(NetActorState state) { }
        #endregion
    }
}
