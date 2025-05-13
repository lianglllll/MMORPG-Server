using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.WoodenDummy.Impl
{
    public class WoodenDummyMonsterAIState_Hurt : WoodenDummyMonsterAIState
    {
        private float hurtWaitTime = 2f;
        private float curWaitTime;

        public override void Enter()
        {
            curWaitTime = hurtWaitTime;
        }
        public override void Update(float deltaTime)
        {
            curWaitTime -= deltaTime;
            if (curWaitTime <= 0f)
            {
                monsterAI.ChangeState(MonsterState.Idle);
            }
        }
    }
}
