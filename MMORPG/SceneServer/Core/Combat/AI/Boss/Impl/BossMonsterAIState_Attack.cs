using Common.Summer.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class BossMonsterAIState_Attack : BossMonsterAIState
    {
        public float attackInterval = 1f;
        public float remainAttackCD;

        public override void Enter()
        {
            TryAttack();
        }

        public override void Update(float deltaTime)
        {
            if (!monsterAI.Monster.IsCanAttack())
            {
                goto End;
            }

            remainAttackCD -= deltaTime;
            if (remainAttackCD <= 0)
            {
                if (!monsterAI.IsTargetInRange(monsterAI.maxAttackDistance))
                {
                    if (monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
                    {
                        monsterAI.ChangeState(MonsterAIState.Chase);
                    }
                    else
                    {
                        monsterAI.ClearTarget();
                        monsterAI.ChangeState(MonsterAIState.Patrol);
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
