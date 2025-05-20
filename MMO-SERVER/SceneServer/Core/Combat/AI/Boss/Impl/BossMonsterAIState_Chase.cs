using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class BossMonsterAIState_Chase : BossMonsterAIState
    {
        public override void Enter()
        {
            monsterAI.Monster.StartMoveToPoint(monsterAI.Target.Position, (int)(monsterAI.chaseSpeed));
        }

        public override void Update(float deltaTime)
        {
            if (monsterAI.CheckExceedMaxBrithDistance())
            {
                monsterAI.ChangeState(MonsterAIState.Rturn);
                goto End;
            }

            if (monsterAI.IsTargetInRange(monsterAI.maxAttackDistance))
            {
                monsterAI.ChangeState(MonsterAIState.Attack);
                goto End;
            }
            
            if (!monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
            {
                monsterAI.ChangeState(MonsterAIState.Patrol);
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
