using Common.Summer.Core;
using Common.Summer.Tools;
using SceneServer.Combat.Skills;
using SceneServer.Core.Combat.AI.MonsterAIStateImpl;
using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene.Component;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI
{
    public class MonsterAI : IStateMachineOwner
    {
        protected SceneMonster m_monster;
        protected StateMachine m_stateMachine;
        protected MonsterState m_curState;

        // AI 驱动频率
        private int m_fps;                  
        private float m_cumulativeTime;
        private float m_updateTime;

        // AI 参数
        public float IdleWaitTime = 5f;

        public int patrolSpeed = 1500;
        public Queue<Vector3> patrolPath = new ();

        protected SceneActor m_target;
        public int chaseSpeed = 5000;
        public int maxChaseDistance = 20000;
      
        public int maxAttackDistance = 2500;

        public int FleeSpeed = 5000;

        public Dictionary<SceneActor, float> threatTable = new();
        public Random random = new();

        #region GetSet

        public SceneMonster Monster => m_monster;
        public SceneActor Target => m_target;
        #endregion

        #region 生命周期函数

        public MonsterAI(SceneMonster owner, int fps = 10)
        {
            m_monster = owner;

            m_fps = fps;
            m_cumulativeTime = 0;
            m_updateTime = 1.0f / fps;

            m_stateMachine = new StateMachine();
            m_stateMachine.Init(this);

            // tmp 模拟一下路径
            patrolPath.Enqueue(new Vector3 { x = Monster.m_initPosition.x, y = Monster.m_initPosition.y, z = Monster.m_initPosition.z});
            patrolPath.Enqueue(new Vector3 { x = Monster.m_initPosition.x + 10000, y = Monster.m_initPosition.y, z = Monster.m_initPosition.z});

            ChangeState(MonsterState.Patrol);
        }
        public  void Update(float deltaTime)
        {
            m_cumulativeTime += deltaTime;
            if (m_cumulativeTime > m_updateTime)
            {
                // UpdateThreatTable();
                // CheckEnvironmentalHazards();
                m_stateMachine.Update(m_cumulativeTime);

                // 推动ai的移动
                m_monster.Moving(m_cumulativeTime);

                m_cumulativeTime = 0;
            }
        }

        #endregion

        #region 其他通用的工具

        public void ChangeState(MonsterState state, bool reCurrstate = false)
        {
            if (m_curState == state && !reCurrstate) return;
            m_curState = state;

            Log.Information("[monsterState]:{0}", m_curState.ToString());

            switch (m_curState)
            {
                case MonsterState.Patrol:
                    m_stateMachine.ChangeState<MonsterAIState_Patrol>(reCurrstate);
                    break;
                case MonsterState.Chase:
                    m_stateMachine.ChangeState<MonsterAIState_Chase>(reCurrstate);
                    break;
                case MonsterState.Death:
                    m_stateMachine.ChangeState<MonsterAIState_Death>(reCurrstate);
                    break;
                case MonsterState.Flee:
                    m_stateMachine.ChangeState<MonsterAIState_Flee>(reCurrstate);
                    break;
                case MonsterState.Hurt:
                    m_stateMachine.ChangeState<MonsterAIState_Hurt>(reCurrstate);
                    break;
                case MonsterState.Attack:
                    m_stateMachine.ChangeState<MonsterAIState_Attack>(reCurrstate);
                    break;
            }
        }
        private void CheckEnvironmentalHazards()
        {
/*            if (IsInDangerArea())
            {
                stateMachine.ChangeState<FleeState>();
            }*/
        }
        private void UpdateThreatTable()
        {
            // 自动衰减仇恨值
            foreach (var key in threatTable.Keys.ToList())
            {
                threatTable[key] *= 0.95f;
                if (threatTable[key] < 0.1f) threatTable.Remove(key);
            }

            // 自动选择最高仇恨目标
            if(threatTable.Count > 0)
            {
                m_target = threatTable.OrderByDescending(p => p.Value).FirstOrDefault().Key;
            }
            else
            {
                m_target = null;
            }
        }

        public void FindNearestTarget()
        {
            var views = AreaEntitiesFinder.GetEntitiesInSectorAroundSceneActor(Monster, 90, maxChaseDistance);
            var chr = views.OfType<SceneCharacter>().FirstOrDefault((a) => !a.IsDeath, null);
            if (chr != null)
            {
                m_target = chr;
            }
        }
        public void ClearTarget()
        {
            m_target = null;
        }
        public bool IsTargetInRange(float range)
        {
            bool result = false; 
            // 目标为空
            if(m_target == null)
            {
                goto End;
            }
            // 目标已经离开当前场景
            if (SceneEntityManager.Instance.GetSceneEntityById(Target.EntityId) == null)
            {
                m_target = null;
                goto End;
            }
            // 目标已经死亡
            if (m_target.IsDeath)
            {
                m_target = null;
                goto End;
            }
            // 目标超出检测距离
            float distance = Vector3.Distance(m_monster.Position, m_target.Position);
            if (distance > range)
            {
                goto End;
            }
            result = true;
        End:
            return result;
        }
        public void CheckNeedRestore_HpAndMp()
        {
            if (m_monster.CurHP < m_monster.MaxHP)
            {
                m_monster.ChangeHP((int)(m_monster.MaxHP * 0.1f));
            }
            if(m_monster.CurMP < m_monster.MaxMP)
            {
                m_monster.ChangeMP((int)(m_monster.MaxMP * 0.1f));
            }
        }

        #endregion
    }
}
