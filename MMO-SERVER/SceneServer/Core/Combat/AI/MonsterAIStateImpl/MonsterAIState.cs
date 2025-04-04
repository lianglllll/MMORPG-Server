using SceneServer.Core.Model.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterAIStateImpl
{
    public enum MonsterState
    {
        None,
        Patrol,
        Chase,
        Attack,
        Dead,
        Flee,
        Hurt
    }

    public abstract class MonsterAIState : StateBase
    {
        protected MonsterAI monsterAI;

        public override void Init(IStateMachineOwner owner)
        {
            monsterAI = owner as MonsterAI;
        }

    }
}
