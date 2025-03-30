using Common.Summer.Core;
using System.Collections.Generic;

namespace SceneServer.Core.Combat.AI.FSM
{
    public class FSM<T>
    {
        public T m_param;//共享参数
        public IState<T> curState;
        public string curStateId;
        private Dictionary<string, IState<T>> stateDict = new Dictionary<string, IState<T>>();
        private int m_fps = 5;    //每秒更新次数
        private float m_cumulativeTime;
        private float m_updateTime;

        public FSM() { }

        public FSM(T param,int fps)
        {
            m_param = param;
            m_fps = fps;
            m_cumulativeTime = 0;
            m_updateTime = 1 / fps;
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

        public void Update(float deltaTime)
        {
            m_cumulativeTime += deltaTime;
            if (m_cumulativeTime > m_updateTime)
            {
                m_cumulativeTime = 0;
                curState?.OnUpdate();
            }
        }

    }
}
