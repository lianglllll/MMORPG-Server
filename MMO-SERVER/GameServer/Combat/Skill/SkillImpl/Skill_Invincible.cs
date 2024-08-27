using GameServer.Buffs.BuffImplement;
using GameServer.Model;
using GameServer.Skills;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat.Skills.SkillImpl
{
    /// <summary>
    /// 无敌
    /// </summary>
    [SkillAttribute(2004)]
    public class Skill_Invincible : Skill
    {
        public Skill_Invincible(Actor owner, int skillId): base(owner, skillId)
        {

        }

        public override void OnActive()
        {
            Owner.buffManager.AddBuff<InvincibleBuff>(Owner);
        }

    }
}
