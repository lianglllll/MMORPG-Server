using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.SceneEntity;
using SceneServer.Combat;
using SceneServer.Core.Combat.Attrubute;
using SceneServer.Utils;

namespace SceneServer.Core.Model.Actor
{
    public class SceneActor : SceneEntity
    {
        public UnitDefine? m_define;
        protected AttributeManager? m_attributeManager;
        protected string? m_actorName;
        protected NetActorNode m_netActorNode;

        // skill
        protected Skill m_curUseSkill;
        protected SkillSpell m_skillSpell;

        public AttrubuteData CurAttrubuteDate => m_attributeManager.final;
        public NetActorState NetActorState => m_netActorNode.NetActorState;
        public bool IsDeath => m_netActorNode.NetActorState == NetActorState.Death; 
        public int CurMP => m_netActorNode.Mp;
        public int CurHP => m_netActorNode.Hp;
        public int CurSpeed => m_netActorNode.Speed;
        public int CurLevel => m_netActorNode.Level;
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
        public bool ChangeMP(int mpDelta)
        {
            return true;
        }


        public void Init(int tId, int level, Vector3Int initPos)
        {
            Init(initPos, Vector3Int.zero, Vector3Int.one);
            m_define = StaticDataManager.Instance.unitDefineDict[tId];
            m_attributeManager = new();
            m_attributeManager.Init(m_define, level);
        }
        public void RecvDamage(Damage damage)
        {
            throw new NotImplementedException();
        }
    }
}
