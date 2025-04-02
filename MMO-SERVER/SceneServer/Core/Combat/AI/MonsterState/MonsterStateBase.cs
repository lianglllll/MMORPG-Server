using SceneServer.Core.Model.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.MonsterState
{
    public class MonsterStateBase : StateBase
    {
        protected SceneMonster m_monster;
        public override void Init(IStateMachineOwner owner)
        {
            m_monster = (SceneMonster)owner;
        }
    }
}
