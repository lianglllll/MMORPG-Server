using GameServer.Combat.Skill;
using GameServer.Model;
using GameServer.Skills;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{

    /// <summary>
    /// 技能管理器，每一个Actor都有独立的技能管理器
    /// </summary>
    public class SkillManager
    {
        private Actor owner;                                //管理器的归属者
        public List<Skill> Skills = new List<Skill>();      //技能队列
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner"></param>
        public SkillManager(Actor owner)
        {
            this.owner = owner;
            InitSkills();
        }
        
        /// <summary>
        /// 初始化技能管理器
        /// </summary>
        public void InitSkills()
        {
            //todo 初始化技能信息，正常是通过读取数据库来加载技能信息的
            //应该开一个表，每个user的character或者monster有那些技能
            //因为这个属于动态数据
            loadSkill(owner.Define.DefaultSkills);
        }

        /// <summary>
        /// 根据技能编号来加载技能
        /// </summary>
        /// <param name="ids"></param>
        private void loadSkill(params int[] ids)
        {
            foreach(int skid in ids)
            {
                if (skid == 0) continue;
                owner.info.Skills.Add(new Proto.SkillInfo() { Id = skid });
                var skill = SkillSanner.CreateSkill(owner, skid);   
                Skills.Add(skill);
                //Log.Information("角色[{0}]加载技能[{1}-{2}]", owner.Name, skill.Define.ID, skill.Define.Name);
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
        public void Update()
        {
            foreach(Skill skill in Skills)
            {
                skill.Update();
            }
        }

    }
}
