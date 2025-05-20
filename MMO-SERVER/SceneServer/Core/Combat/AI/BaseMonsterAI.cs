using Common.Summer.Tools;
using SceneServer.Core.Combat.AI.MonsterAIStateImpl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SceneServer.Core.Combat.AI
{
    public enum MonsterAIState
    {
        None,
        Patrol,
        Chase,
        Attack,
        Death,
        Flee,
        Hurt,
        Rturn,
        Idle
    }

    public class BaseMonsterAI : IStateMachineOwner
    {
        // AI 驱动频率
        private int m_fps;
        private float m_cumulativeTime;
        private float m_updateTime;

        // 状态机
        protected StateMachine m_stateMachine;
        protected MonsterAIState m_curState;

        public BaseMonsterAI(int fps) {
            m_fps = fps;
            m_cumulativeTime = 0;
            m_updateTime = 1.0f / fps;

            m_stateMachine = new StateMachine();
            m_stateMachine.Init(this);
        }
        public void Update(float deltaTime)
        {
            m_cumulativeTime += deltaTime;
            if (m_cumulativeTime > m_updateTime)
            {
                m_stateMachine.Update(deltaTime);
                Dothing(m_cumulativeTime);

                m_cumulativeTime = 0;
            }
        }
        protected virtual void Dothing(float deltaTime)
        {
        }
        public virtual void ChangeState(MonsterAIState state, bool reCurrstate = false) { 
        
        }
    }
}
