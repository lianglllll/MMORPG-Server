using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Combat.Skills.SkillImpl
{
    /// <summary>
    /// 风行术
    /// </summary>
    [SkillAttribute(2003)]
    public class Skill_Fengxingshu : Skill
    {
        public Skill_Fengxingshu(SceneActor owner, int skillId): base(owner, skillId)
        {

        }

        protected override void OnActive()
        {
            //Log.Information("风行术激活");
            if(Target.RealObj is SceneActor actor)
            {
                //actor.buffManager.AddBuff<FengxingshuBuff>(Owner);
            }

        }

    }
}
