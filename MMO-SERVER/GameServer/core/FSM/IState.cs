using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core.FSM
{


    /// <summary>
    /// 状态基础类
    /// </summary>
    public class IState<T>
    {
        public FSM<T> fsm;
        public T param => fsm.param;        //lambda
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }


    }
}
