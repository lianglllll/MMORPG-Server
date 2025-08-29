using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Combat.Skills.SkillImpl
{
    /// <summary>
    /// 无敌
    /// </summary>
    [SkillAttribute(2004)]
    public class Skill_Invincible : Skill
    {
        public Skill_Invincible(SceneActor owner, int skillId): base(owner, skillId)
        {

        }

        protected override void OnActive()
        {
            //Owner.buffManager.AddBuff<InvincibleBuff>(Owner);
        }

    }
}
