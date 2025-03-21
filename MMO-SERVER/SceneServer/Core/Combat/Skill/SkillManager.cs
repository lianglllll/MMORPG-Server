using HS.Protobuf.Combat.Skill;
using SceneServer.Core.Model.Actor;
using SceneServer.Utils;

namespace SceneServer.Core.Combat.Skills
{
    /// <summary>
    /// 技能管理器，每一个Actor都有独立的技能管理器
    /// </summary>
    public class SkillManager
    {
        private SceneActor m_owner;                                // 管理器的归属者
        public List<Skill> Skills = new();                         // 技能队列

        public bool Init(SceneActor owner)
        {
            m_owner = owner;
            var def =  StaticDataManager.Instance.weaponSkillArsenalDefineDict[m_owner.m_define.weaponSkillArsenalId];
            _LoadSkillsByIds(def.SkillIds.ToList());
            _LoadSkillsByIds(m_owner.EquippedSkillIds);
            return true;
        }
        public void Update(float deltaTime)
        {
            foreach (Skill skill in Skills)
            {
                skill.Update(deltaTime);
            }
        }

        private void _LoadSkillsByIds(List<int> ids)
        {
            foreach(int skid in ids)
            {
                if (skid == 0) continue;
                var skill = SkillSanner.CreateSkill(m_owner, skid);   
                Skills.Add(skill);
            }
        }
        public Skill GetSkillById(int skillId)
        {
            foreach (var skill in Skills) { 
                if(skill.Define.ID == skillId)
                {
                    return skill;
                }
            }
            return null;
        }
    }
}
