using Common.Summer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class MonsterAIState_Attack : MonsterAIState
    {
        public float attackInterval = 2f;
        public float remainAttackCD;


        public override void Enter()
        {
            TryAttack();
        }

        public override void Update(float deltaTime)
        {
            remainAttackCD -= deltaTime;

            if (remainAttackCD <= 0)
            {
                if (!monsterAI.IsTargetInRange(monsterAI.maxAttackDistance))
                {
                    if (monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
                    {
                        monsterAI.ChangeState(MonsterState.Chase);
                    }
                    else
                    {
                        monsterAI.ClearTarget();
                        monsterAI.ChangeState(MonsterState.Patrol);
                    }
                    goto End;
                }

                TryAttack();
            }

        End:
            return;
        }

        private void TryAttack()
        {
            monsterAI.Monster.Attack(monsterAI.Target);
            remainAttackCD = attackInterval;
        }
    }
}
