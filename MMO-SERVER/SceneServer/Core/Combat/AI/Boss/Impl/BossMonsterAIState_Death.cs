using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public class BossMonsterAIState_Death : BossMonsterAIState
    {
        public override void Enter()
        {

        }
        public override void Update(float deltatime)
        {
            // 检测死亡状态退出
            if (!monsterAI.Monster.IsDeath)
            {
                monsterAI.ChangeState(MonsterState.Patrol);
            }
        }
    }
}
