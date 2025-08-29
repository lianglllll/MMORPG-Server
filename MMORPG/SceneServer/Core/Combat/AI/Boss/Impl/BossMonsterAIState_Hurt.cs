using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class BossMonsterAIState_Hurt : BossMonsterAIState
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
                if (((float)monsterAI.Monster.CurHP / monsterAI.Monster.MaxHP) < 0.05f)
                {
                    monsterAI.ChangeState(MonsterAIState.Flee);
                }
                else if(monsterAI.IsTargetInRange(monsterAI.maxAttackDistance))
                {
                    monsterAI.ChangeState(MonsterAIState.Attack);
                }
                else if (monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
                {
                    monsterAI.ChangeState(MonsterAIState.Attack);
                }
                else
                {
                    monsterAI.ChangeState(MonsterAIState.Patrol);
                }
            }
        }

    }
}
