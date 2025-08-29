using Common.Summer.Core;
using Common.Summer.Tools;
using SceneServer.Combat.Skills;
using SceneServer.Core.Combat.AI.MonsterAi.MonsterAIStateImpl;
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
    public class BossMonsterAI : BaseMonsterAI
    {
        protected SceneMonster m_owner;

        #region AI 参数

        // patrol
        public float IdleWaitTime = 5f;
        public int patrolSpeed = 1500;
        public Queue<Vector3> m_patrolPathQueue;

        // chase
        protected SceneActor m_target;
        public Dictionary<SceneActor, float> threatTable = new();   // 仇恨表
        public int chaseSpeed = 5000;
        public int maxChaseDistance = 20000;
        public int maxBrithDistance = 35000; 
        // attack
        public int maxAttackDistance = 2500;
        // flee
        public int FleeSpeed = 5000;

        #endregion

        #region GetSet
        public SceneMonster Monster => m_owner;
        public SceneActor Target => m_target;
        #endregion

        #region 生命周期函数
        public BossMonsterAI(SceneMonster owner, List<Vector3> patrolPath, int fps = 10) : base(fps)
        {
            m_owner = owner;

            // 巡逻路径
            m_patrolPathQueue = new();
            foreach (var item in patrolPath)
            {
                m_patrolPathQueue.Enqueue(item);
            }

            // 默认
            ChangeState(MonsterAIState.Patrol);
        }
        protected override void Dothing(float deltaTime)
        {
            // UpdateThreatTable();
            // CheckEnvironmentalHazards();

            // 推动ai的移动
            m_owner.Moving(deltaTime);
        }
        #endregion

        #region 其他通用的工具

        public Random random = new();

        public override void ChangeState(MonsterAIState state, bool reCurrstate = false)
        {
            if (m_curState == state && !reCurrstate) return;
            m_curState = state;

            Log.Information("[monsterState]:{0}", m_curState.ToString());

            switch (m_curState)
            {
                case MonsterAIState.Patrol:
                    m_stateMachine.ChangeState<BossMonsterAIState_Patrol>(reCurrstate);
                    break;
                case MonsterAIState.Chase:
                    m_stateMachine.ChangeState<BossMonsterAIState_Chase>(reCurrstate);
                    break;
                case MonsterAIState.Death:
                    m_stateMachine.ChangeState<BossMonsterAIState_Death>(reCurrstate);
                    break;
                case MonsterAIState.Flee:
                    m_stateMachine.ChangeState<BossMonsterAIState_Flee>(reCurrstate);
                    break;
                case MonsterAIState.Hurt:
                    m_stateMachine.ChangeState<BossMonsterAIState_Hurt>(reCurrstate);
                    break;
                case MonsterAIState.Attack:
                    m_stateMachine.ChangeState<BossMonsterAIState_Attack>(reCurrstate);
                    break;
                case MonsterAIState.Rturn:
                    m_stateMachine.ChangeState<BossMonsterAIState_ReturnBrith>(reCurrstate);
                    break;
                default:
                    m_stateMachine.ChangeState<BossMonsterAIState_Patrol>(reCurrstate);
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
            float distance = Vector3.Distance(m_owner.Position, m_target.Position);
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
            if (m_owner.CurHP < m_owner.MaxHP)
            {
                m_owner.ChangeHP((int)(m_owner.MaxHP * 0.1f));
            }
            if(m_owner.CurMP < m_owner.MaxMP)
            {
                m_owner.ChangeMP((int)(m_owner.MaxMP * 0.1f));
            }
        }

        public bool CheckExceedMaxBrithDistance()
        {
            bool result = false;
            var distance = Vector3.Distance(m_owner.Position, m_owner.m_initPosition);
            if(distance > maxBrithDistance)
            {
                result = true;
            }
        End:
            return result;
        }

        #endregion
    }
}
