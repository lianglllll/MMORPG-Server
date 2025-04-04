using Common.Summer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class MonsterAIState_Patrol : MonsterAIState
    {
        private Vector3 currentDestination;
        private float waitTimer;
        private bool isStartWait;

        public override void Enter()
        {
            GetNextDestination();
            isStartWait = false;
        }
        public override void Update(float deltaTime)
        {
            monsterAI.FindNearestTarget();

            // 优先级：危险 > 战斗 > 巡逻
            if (monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
            {
                monsterAI.ChangeState(MonsterState.Chase);
                goto End;
            }

            // 回血回蓝
            monsterAI.CheckNeedRestore_HpAndMp();

            // 一段巡逻结束
            if (!monsterAI.Monster.IsMoving && !isStartWait)
            {
                waitTimer = monsterAI.IdleWaitTime;
                isStartWait = true;
            }

            // 下一段巡逻
            if (isStartWait)
            {
                waitTimer -= deltaTime;
                if (waitTimer <= 0)
                {
                    GetNextDestination();
                    isStartWait = false;
                }
            }


        End:
            return;
        }
        public override void Exit()
        {
            monsterAI.Monster.StopMove();
        }

        private void GetNextDestination()
        {
            if (monsterAI.patrolPath.Count > 0)
            {
                currentDestination = monsterAI.patrolPath.Dequeue();
                monsterAI.patrolPath.Enqueue(currentDestination);
                monsterAI.Monster.StartMoveToPoint(currentDestination, monsterAI.patrolSpeed);
                waitTimer = monsterAI.random.NextInt64(3, 7); 
            }
        }
    }
}
