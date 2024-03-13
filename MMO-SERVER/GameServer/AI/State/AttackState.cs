using GameServer.core.FSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.State
{
    public class AttackState: IState<Param>
    {
        public AttackState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {

        }

        public override void OnUpdate()
        {

        }
    }
}
