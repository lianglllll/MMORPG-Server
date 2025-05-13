using SceneServer.Core.Combat.AI.MonsterAi.MonsterAIStateImpl;
using SceneServer.Core.Combat.AI.MonsterAIStateImpl;
using SceneServer.Core.Combat.AI.WoodenDummy.Impl;
using SceneServer.Core.Model.Actor;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI.WoodenDummy
{
    public class WoodenDummyMonsterAI : BaseMonsterAI
    {
        private SceneMonster m_owner;
        public WoodenDummyMonsterAI(SceneMonster owner, int fps = 10) : base(fps)
        {
            m_owner = owner;
            ChangeState(MonsterState.Idle);
        }

        public SceneMonster Monster => m_owner;

        public override void ChangeState(MonsterState state, bool reCurrstate = false)
        {
            if (m_curState == state && !reCurrstate) return;
            m_curState = state;

            Log.Information("[monsterState]:{0}", m_curState.ToString());

            switch (m_curState)
            {
                case MonsterState.Idle:
                    m_stateMachine.ChangeState<WoodenDummyMonsterAIState_Idle>(reCurrstate);
                    break;
                case MonsterState.Hurt:
                    m_stateMachine.ChangeState<WoodenDummyMonsterAIState_Hurt>(reCurrstate);
                    break;                
                case MonsterState.Death:
                    m_stateMachine.ChangeState<WoodenDummyMonsterAIState_Death>(reCurrstate);
                    break;
                default:
                    m_stateMachine.ChangeState<WoodenDummyMonsterAIState_Idle>(reCurrstate);
                    break;
            }
        }
    }
}
