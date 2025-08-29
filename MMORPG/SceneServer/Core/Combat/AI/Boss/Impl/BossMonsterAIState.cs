using Common.Summer.Tools;
using SceneServer.Core.Model.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public abstract class BossMonsterAIState : StateBase
    {
        protected BossMonsterAI monsterAI;

        public override void Init(IStateMachineOwner owner)
        {
            monsterAI = owner as BossMonsterAI;
        }

    }
}
