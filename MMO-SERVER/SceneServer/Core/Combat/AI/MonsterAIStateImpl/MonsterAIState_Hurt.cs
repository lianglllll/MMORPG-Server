using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class MonsterAIState_Hurt : MonsterAIState
    {
        private float hurtWaitTime = 1f;
        private float curWaitTime;

        public override void Enter()
        {
            curWaitTime = hurtWaitTime;
        }
        public override void Update(float deltaTime)
        {
            curWaitTime -= deltaTime;
            if(curWaitTime <= 0f)
            {
                if (monsterAI.Monster.CurHP / monsterAI.Monster.MaxHP < 0.1f)
                {
                    monsterAI.ChangeState(MonsterState.Flee);
                }
                else if(monsterAI.IsTargetInRange(monsterAI.maxAttackDistance))
                {
                    monsterAI.ChangeState(MonsterState.Attack);
                }
                else if (monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
                {
                    monsterAI.ChangeState(MonsterState.Attack);
                }
                else
                {
                    monsterAI.ChangeState(MonsterState.Patrol);
                }
            }
        }
        public override void Exit() { 
        }

    }
}
