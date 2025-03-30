using Common.Summer.Core;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene.Component;

namespace SceneServer.Core.Combat.AI.FSM.State
{
    /// <summary>
    /// 追击状态
    /// </summary>
    public class ChaseState : IState<Param>
    {
        private int flag;
        private SceneMonster m_monster;

        public ChaseState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            flag = 1;
            m_monster = param.owner;
        }
        public override void OnUpdate()
        {
            // 追击目标失效切换为返回状态
            if (m_monster.m_target == null || m_monster.m_target.IsDeath || 
                SceneEntityManager.Instance.GetSceneEntityById(m_monster.m_target.EntityId) == null)
            {
                m_monster.m_target = null;
                fsm.ChangeState("return");
                goto End;
            }

            // 计算距离
            float brithDistance = Vector3.Distance(m_monster.m_initPosition, m_monster.Position);
            float targetDistance = Vector3.Distance(m_monster.m_target.Position, m_monster.Position);

            // 当超过我们的活动范围或者追击范围，切换返回状态
            if (brithDistance > param.walkRange || targetDistance > param.chaseRange)
            {
                m_monster.m_target = null;
                fsm.ChangeState("return");
                goto End;
            }

            // 攻击距离不够，我们继续靠近目标
            if (targetDistance > 2000)
            {
                m_monster.StartMoveToPoint(m_monster.m_target.Position);
                goto End;
            }

            // 在技能后摇结束之前，我们不能再次攻击
            if (m_monster.CurUseSkill == null)
            {
                //进入攻击状态
                fsm.ChangeState("attack");
            }

        End:
            return;
        }
    }
}
