using HS.Protobuf.Combat.Skill;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Combat.Skills
{

    /// <summary>
    /// 技能管理器，每一个Actor都有独立的技能管理器
    /// </summary>
    public class SkillManager
    {
        private SceneActor owner;                                //管理器的归属者
        public List<Skill> Skills = new();                      //技能队列
        
        public bool Init(SceneActor owner)
        {
            this.owner = owner;
            InitSkills();
            return true;
        }
        
        /// <summary>
        /// 初始化技能管理器
        /// </summary>
        public void InitSkills()
        {
            //todo 初始化技能信息，正常是通过读取数据库来加载技能信息的
            //应该开一个表，每个user的character或者monster有那些技能
            //因为这个属于动态数据
            loadSkill(owner.EquippedSkillIds);
        }

        /// <summary>
        /// 根据技能编号来加载技能
        /// </summary>
        /// <param name="ids"></param>
        private void loadSkill(List<int> ids)
        {
            List<SkillInfo> list = new List<SkillInfo>();
            foreach(int skid in ids)
            {
                if (skid == 0) continue;
                var skillinfo = new SkillInfo() { Id = skid };
                list.Add(skillinfo);
                var skill = SkillSanner.CreateSkill(owner, skid);   
                Skills.Add(skill);
            }
        }

        /// <summary>
        /// 根据技能id获取某个技能
        /// </summary>
        /// <param name="skillId"></param>
        /// <returns></returns>
        public Skill GetSkill(int skillId)
        {
            foreach (var skill in Skills) { 
                if(skill.Define.ID == skillId)
                {
                    return skill;
                }
            }
            return null;
        }

        /// <summary>
        /// 推动每一个技能运转  
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach(Skill skill in Skills)
            {
                skill.Update(deltaTime);
            }
        }

    }
}
