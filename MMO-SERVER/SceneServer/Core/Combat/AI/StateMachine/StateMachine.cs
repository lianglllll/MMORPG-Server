using System;
using System.Collections.Generic;

namespace SceneServer.Core.Combat.AI
{
    public interface IStateMachineOwner { }

    public class StateMachine
    {
        private IStateMachineOwner owner;
        private StateBase curState;
        private Dictionary<Type, StateBase> stateDict = new();
        public StateBase CurState { get => curState; }

        public void Init(IStateMachineOwner owner)
        {
            this.owner = owner;
        }
        public void UnInit()
        {
            if (curState != null)
            {
                curState.Exit();
            }
        }
        public void Stop()
        {
            curState.Exit();

            foreach (var item in stateDict.Values)
            {
                item.UnInit();
            }
            stateDict.Clear();
        }
        private StateBase GetState<T>() where T : StateBase, new()
        {
            Type type = typeof(T);
            if (!stateDict.TryGetValue(type, out var state))
            {
                state = new T();
                state.Init(owner);
                stateDict.Add(type, state);
            }
            return state;
        }

        public bool ChangeState<T>(bool reCurrstate = false) where T : StateBase, new()
        {
            if (curState != null && curState.GetType() == typeof(T) && !reCurrstate) return false;

            //退出当前状态
            if (curState != null)
            {
                curState.Exit();
            }

            //进入新状态
            curState = GetState<T>();
            curState.Enter();

            return false;
        }
        public virtual void Update()
        {
            curState?.Update();
        }
    }
}
