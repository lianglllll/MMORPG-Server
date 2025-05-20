using Common.Summer.Core;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.AI.MonsterAIStateImpl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAi.MonsterAIStateImpl
{
    public class BossMonsterAIState_ReturnBrith : BossMonsterAIState
    {
        public override void Enter()
        {
            monsterAI.Monster.StartMoveToPoint(monsterAI.Monster.m_initPosition, monsterAI.patrolSpeed);
        }

        public override void Update(float deltaTime)
        {
            // 接近到出生点就切换为巡逻状态
            if (Vector3.Distance(monsterAI.Monster.m_initPosition, monsterAI.Monster.Position) < 100)
            {
                monsterAI.ChangeState(MonsterAIState.Patrol);
                goto End;
            }

        End:
            return;
        }
    }
}
