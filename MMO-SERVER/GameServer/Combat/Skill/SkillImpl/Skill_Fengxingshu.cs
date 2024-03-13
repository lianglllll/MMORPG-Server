using GameServer.Buffs.BuffImplement;
using GameServer.Model;
using GameServer.Skills;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat.Skill.SkillImpl
{
    /// <summary>
    /// 风行术
    /// </summary>
    [Skill(2003)]
    public class Skill_Fengxingshu : Skill
    {
        public Skill_Fengxingshu(Actor owner, int skillId): base(owner, skillId)
        {

        }

        public override void OnActive()
        {
            //Log.Information("风行术激活");
            if(Target.RealObj is Actor actor)
            {
                actor.buffManager.AddBuff<FengxingshuBuff>(Owner);
            }

        }

    }
}
