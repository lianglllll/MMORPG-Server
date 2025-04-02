using SceneServer.Core.Combat.AI.MonsterState;
using SceneServer.Core.Model.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI
{
    public class WoodenStakeAI : AIBase
    {
        protected SceneMonster m_monster;
        protected StateMachine m_stateMachine;

        private int m_fps;    //每秒更新次数
        private float m_cumulativeTime;
        private float m_updateTime;

        public WoodenStakeAI(SceneMonster owner, int fps = 5) : base(owner)
        {
            m_monster = owner;

            m_fps = fps;
            m_cumulativeTime = 0;
            m_updateTime = 1 / fps;

            m_stateMachine = new StateMachine();
            m_stateMachine.Init(m_monster);

            m_stateMachine.ChangeState<MonsterState_Idle>();
        }

        public override void Update(float deltaTime)
        {
            m_cumulativeTime += deltaTime;
            if (m_cumulativeTime > m_updateTime)
            {
                m_stateMachine.Update();
                m_cumulativeTime = 0;
            }
        }
    }
}
