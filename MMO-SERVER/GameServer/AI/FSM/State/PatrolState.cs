using GameServer.Combat;
using GameServer.Model;
using System.Linq;
using Proto;
using Common.Summer.Core;

namespace GameServer.AI.FSM.State
{
    /// <summary>
    /// monster的巡逻行为
    /// 1.逻辑：原地罚站(waitTime)->巡逻(起始就是走到某个坐标点)->原地罚站->...
    /// 2.在巡逻这个行为时间内，如果ai的状态不是满的，就缓慢恢复状态
    /// </summary>
    public class PatrolState : IState<Param>
    {
        float lastTime = MyTime.time;                 //ai上次开始巡逻的时间
        private static float waitTime = 10f;        //ai原地等待的时间

        float lastRestoreHpMpTime = MyTime.time;      //用于重置回复状态的时间点
        private static float restoreWaitTime = 1f;

        public PatrolState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
            param.owner.StopMove();
        }

        public override void OnUpdate()
        {
            var monster = param.owner;

            //查询viewRange内的玩家，如果有就切换追击状态
            var views = AreaEntitiesFinder.GetEntitiesInCircleAroundEntity(monster, param.viewRange, false);
            var chr = views.OfType<Character>().FirstOrDefault((a) => !a.IsDeath, null);
            if (chr != null)
            {
                monster.target = chr;
                fsm.ChangeState("chase");
                return;
            }

            //到了需要移动位置的时间
            if (monster.State == ActorState.Idle)
            {
                //到时间刷新了（每10秒刷新一次）
                if (lastTime + waitTime < MyTime.time)
                {
                    lastTime = MyTime.time;
                    waitTime = (float)(param.rand.NextDouble() * 20f) + 10f;
                    //移动到随机位置
                    var target = monster.RandomPointWithBirth(param.walkRange);
                    monster.StartMoveTo(target);
                }
            }

            //当actor状态不健康的时候回血回蓝
            if (!monster.IsDeath && monster.Check_HpAndMp_Needs())
            {
                if (lastRestoreHpMpTime + restoreWaitTime < MyTime.time)
                {
                    lastRestoreHpMpTime = MyTime.time;
                    monster.Restore_HpAndMp();
                }

            }

        }


    }
}
