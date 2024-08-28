using Common.Summer.GameServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.FSM
{
    public class FSM<T>
    {
        public T param;//共享参数
        public IState<T> curState;
        public string curStateId;
        private Dictionary<string, IState<T>> stateDict = new Dictionary<string, IState<T>>();
        private int fps = 5;    //每秒更新次数
        private float _lastUpdateTime = MyTime.time;

        public FSM() { }

        public FSM(T param,int fps)
        {
            this.param = param;
            this.fps = fps;
        }

        //添加状态
        public void AddState(string stateId, IState<T> state)
        {
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

            curState?.OnExit();
            curStateId = stateId;
            curState = stateDict[stateId];
            curState.OnEnter();
        }

        public void Update()
        {
            if(MyTime.time - _lastUpdateTime > 1 / fps)
            {
                _lastUpdateTime = MyTime.time;
                curState?.OnUpdate();
            }
        }

    }
}
