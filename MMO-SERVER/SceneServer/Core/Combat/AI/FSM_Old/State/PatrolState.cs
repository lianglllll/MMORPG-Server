using System.Linq;
using HS.Protobuf.SceneEntity;
using Common.Summer.Core;
using SceneServer.Combat.Skills;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Combat.AI.FSM.State
{
    /// <summary>
    /// monster的巡逻行为
    /// 1.逻辑：原地罚站(waitTime)->巡逻(起始就是走到某个坐标点)->原地罚站->...
    /// 2.在巡逻这个行为时间内，如果ai的状态不是满的，就缓慢恢复状态
    /// </summary>
    public class PatrolState : IState<Param>
    {
        private float m_lastPatrolTime;                       // ai上次开始巡逻的时间
        private static float m_inplaceWaitTime = 10000000f;         // ai随机在原地等待的时间

        private float m_lastRestoreHpMpTime = MyTime.time;    // 用于重置回复状态的时间点
        private static float restoreWaitTime = 1f;

        public PatrolState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }
        public override void OnEnter()
        {
            param.owner.StopMove();
            m_lastPatrolTime = MyTime.time;
            m_lastRestoreHpMpTime = MyTime.time;
        }
        public override void OnUpdate()
        {
            var monster = param.owner;

            // 查询viewRange内是否有玩家，如果有就切换追击状态
            var views = AreaEntitiesFinder.GetEntitiesInCircleAroundSceneActor(monster, param.viewRange, false);
            var chr = views.OfType<SceneCharacter>().FirstOrDefault((a) => !a.IsDeath, null);
            if (chr != null)
            {
                monster.m_target = chr;
                fsm.ChangeState("chase");
                goto End;
            }

            // 到了需要移动位置的时间
            if (monster.NetActorState == NetActorState.Idle)
            {
                float curTime = MyTime.time;
                // 到时间刷新了（每10秒刷新一次）
                if (m_lastPatrolTime + m_inplaceWaitTime < curTime)
                {
                    // 时间参数更新
                    m_lastPatrolTime = curTime;
                    m_inplaceWaitTime = (float)(param.rand.NextDouble() * 20f) + 10f;

                    // 移动到随机位置
                    var target = monster.RandomPointWithBirth(param.walkRange);
                    monster.StartMoveToPoint(target);
                }
            }

            // 当actor状态不健康的时候回血回蓝
            if (!monster.IsDeath && monster.Check_HpAndMp_Needs())
            {
                if (m_lastRestoreHpMpTime + restoreWaitTime < MyTime.time)
                {
                    m_lastRestoreHpMpTime = MyTime.time;
                    monster.Restore_HpAndMp();
                }

            }

        End:
            return;
        }
    }
}
