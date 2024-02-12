using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core.FSM
{
    public class FSM<T>
    {

        public T param;//共享参数

        public IState<T> curState;
        public string curStateId;
        private Dictionary<string, IState<T>> stateDict = new Dictionary<string, IState<T>>();


        public FSM() { }

        public FSM(T param)
        {
            this.param = param;
        }

        //添加状态
        public void AddState(string stateId, IState<T> state)
        {
            if(curState == null)
            {
                curState = state;
                curStateId = stateId;
            }

            stateDict[stateId] = state;
            state.fsm = this;
        }

        //移除状态
        public void RemoveState(string stateId)
        {
            if (stateDict.ContainsKey(stateId))
            {
                stateDict[stateId].fsm = null;
                stateDict.Remove(stateId);
            }
        }

        //状态切换
        public void ChangeState(string stateId)
        {

            if (curStateId == stateId) return;
            if (!stateDict.ContainsKey(stateId)) return;
            if(curState != null)
            {
                curState.OnExit();
            }
            curStateId = stateId;
            curState = stateDict[stateId];
            curState.OnEnter();
        }

        public  void Update()
        {
            if (curState != null)
            {
                curState.OnUpdate();
            }
        }

    }
}
