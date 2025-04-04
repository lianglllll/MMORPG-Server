using Common.Summer.Core;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class MonsterAIState_Flee : MonsterAIState
    {
        private Vector3 fleeDirection;
        private float changeDirInterval;
        private float changeDirRemainTime;
        private float fleeDuration;

        public override void Enter()
        {
            fleeDuration = monsterAI.random.NextInt64(5, 8);
            changeDirInterval = fleeDuration * 0.2f;
            ChangeDir();

        }
        public override void Update(float deltatime)
        {

            if (monsterAI.Monster.CurHP / monsterAI.Monster.MaxHP > 0.5f)
            {
                monsterAI.ChangeState(MonsterState.Patrol);
                goto End;
            }

            fleeDuration -= deltatime;
            if(fleeDuration <= 0)
            {
                monsterAI.ChangeState(MonsterState.Chase);
                goto End;
            }
            if (!monsterAI.IsTargetInRange(monsterAI.maxChaseDistance))
            {
                monsterAI.ClearTarget();
                monsterAI.ChangeState(MonsterState.Patrol);
                goto End;
            }

            changeDirRemainTime -= deltatime;
            if (changeDirRemainTime <= 0)
            {
                ChangeDir();
            }

        End:
            return;
        }
        private Vector3 CalculateFleeDirection()
        {
            // 综合危险源和玩家位置计算最佳逃跑方向
            Vector3 tmp = (monsterAI.Monster.Position - monsterAI.Target.Position);
            return tmp.normalized;
        }
        private void ChangeDir()
        {
            fleeDirection = CalculateFleeDirection().normalized;
            var nextPos = fleeDirection * monsterAI.FleeSpeed * changeDirInterval;
            monsterAI.Monster.StartMoveToPoint(nextPos, monsterAI.FleeSpeed);
            changeDirRemainTime = changeDirInterval;
        }

    }
}
