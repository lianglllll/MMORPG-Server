using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class MonsterAIState_Chase : MonsterAIState
    {
        public override void Enter()
        {
            monsterAI.Monster.StartMoveToPoint(monsterAI.Target.Position, (int)(monsterAI.chaseSpeed));
        }

        public override void Update(float deltaTime)
        {
            if (monsterAI.IsTargetInRange(monsterAI.maxAttackDistance))
            {
                monsterAI.ChangeState(MonsterState.Attack);
                goto End;
            }
            
            if (!monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
            {
                monsterAI.ChangeState(MonsterState.Patrol);
                monsterAI.ClearTarget();
                goto End;
            }

            monsterAI.Monster.StartMoveToPoint(monsterAI.Target.Position, monsterAI.chaseSpeed);
        
        End:
            return;
        }

        public override void Exit()
        {
            monsterAI.Monster.StopMove();
        }
    }
}
