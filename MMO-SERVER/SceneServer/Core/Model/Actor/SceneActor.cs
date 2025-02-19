using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.Attrubute;
using SceneServer.Utils;
using SceneServer.Core.Combat.Skills;
using GameServer.Buffs;

namespace SceneServer.Core.Model.Actor
{
    public class SceneActor : SceneEntity
    {
        public UnitDefine? m_define;

        protected NetActorNode? m_netActorNode;
        protected AttributeManager m_attributeManager = new();

        // skill
        protected Skill? m_curUseSkill;
        protected SkillSpell m_skillSpell = new();
        protected SkillManager m_skillManager = new();

        // buff
        protected BuffManager m_buffManager = new();

        public void Init(NetActorNode netActorNode)
        {
            Init(netActorNode.Transform.Position, Vector3Int.zero, Vector3Int.one);
            m_define = StaticDataManager.Instance.unitDefineDict[netActorNode.ProfessionId];
            m_netActorNode = netActorNode;
            m_attributeManager.Init(m_define, m_netActorNode.Level);
            netActorNode.MaxHp = m_attributeManager.final.MaxHP;
            netActorNode.MaxMp = m_attributeManager.final.MaxMP;
            netActorNode.Hp = MaxHP;
            netActorNode.Mp = MaxMP;
            netActorNode.Speed = m_attributeManager.final.Speed;
            netActorNode.NetActorMode = NetActorMode.None;
            netActorNode.NetActorState = NetActorState.None;
            netActorNode.NetActorSmallState = NetActorSmallState.None;
            m_curUseSkill = null;
            m_skillSpell.Init(this);
            m_skillManager.Init(this);
            m_buffManager.Init(this);
        }
        public override void Update(float deltaTime)
        {
            m_skillManager.Update(deltaTime);
            m_buffManager.Update(deltaTime);
        }

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
        public int CurHP => m_netActorNode.Hp;
        public int CurMP => m_netActorNode.Mp;
        public int MaxHP => m_netActorNode.Hp;
        public int MaxMP => m_netActorNode.Mp;
        public int Speed
        {
            get => m_netActorNode.Speed;
            set
            {
                m_netActorNode.Speed = value;
            }
        }
        public int CurLevel => m_netActorNode.Level;
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
            return null;
        }
        public SkillSpell SkillSpell => m_skillSpell;
        public List<int> EquippedSkillIds => m_netActorNode.EquippedSkills.ToList<int>();

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
            return true;
        }
        public bool ChangeMP(int mpDelta)
        {
            return true;
        }
        public bool ChangeHP(int hpDelta)
        {
            return true;
        }
        public void RecvDamage(Damage damage)
        {
            throw new NotImplementedException();
        }
    }
}
