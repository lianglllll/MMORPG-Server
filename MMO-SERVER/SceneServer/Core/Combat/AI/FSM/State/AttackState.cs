using Common.Summer.Core;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene.Component;

namespace SceneServer.Core.Combat.AI.FSM.State
{
    
    public class AttackState : IState<Param>
    {
        private Skill curUsedSkill;
        private float afterWaitTime;
        private SceneMonster m_monster;

        public AttackState(FSM<Param> fsm)
        {
            this.fsm = fsm;
            m_monster = param.owner;
        }

        public override void OnEnter()
        {
            //目标是否有效，无效就返回了
            if (m_monster.m_target == null || m_monster.m_target.IsDeath || 
                SceneEntityManager.Instance.GetSceneEntityById(m_monster.m_target.EntityId) != null)
            {
                m_monster.m_target = null;
                fsm.ChangeState("return");
                goto End;
            }

            //找一个技能，打击目标
            curUsedSkill = param.owner.Attack(param.owner.m_target);

            //攻击失败的惩罚和攻击后摇时间
            if (curUsedSkill == null)
            {
                afterWaitTime = 3f;
                m_monster.StopMove();
            }
            else
            {
                afterWaitTime = curUsedSkill.Define.IntonateTime + curUsedSkill.Define.Duration;
            }
        End:
            return;
        }

        public override void OnUpdate()
        {
            afterWaitTime -= MyTime.deltaTime;

            //技能释放后的后摇时间，这里傻站着不动
            if (afterWaitTime <= 0)
            {
                afterWaitTime = 0;
                if (m_monster.HaveTarget())
                {
                    fsm.ChangeState("chase");
                    goto End;
                }
                else
                {
                    fsm.ChangeState("return");
                    goto End;
                }
            }
        End:
            return;
        }
    }
}
