using Common.Summer.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.WoodenDummy.Impl
{
    public class WoodenDummyMonsterAIState : StateBase
    {
        protected WoodenDummyMonsterAI monsterAI;

        public override void Init(IStateMachineOwner owner)
        {
            monsterAI = owner as WoodenDummyMonsterAI;
        }
    }
}
