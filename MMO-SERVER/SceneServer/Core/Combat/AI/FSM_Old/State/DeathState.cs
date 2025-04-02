using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.FSM.State
{
    /// <summary>
    /// 死亡状态
    /// </summary>
    public class DeathState : IState<Param>
    {
        public DeathState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
        }
        public override void OnUpdate()
        {
            //寻找退出死亡状态时间点
            if (!param.owner.IsDeath)
            {
                fsm.ChangeState("patrol");
            }
        }
    }
}
