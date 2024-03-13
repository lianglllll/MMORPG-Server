using GameServer.core.FSM;
using GameServer.Manager;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.State
{
    /// <summary>
    /// 巡逻状态
    /// </summary>
    public class PatrolState : IState<Param>
    {

        float lastTime = Time.time;             //用于重置下次巡逻的位置
        private static float waitTime = 10f;

        float lastRestoreHpMpTime = Time.time;  //用于重置回复状态的时间点
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
            var chr = EntityManager.Instance.GetGetNearEntitys(monster.SpaceId, monster.Position, param.viewRange).FirstOrDefault(a => !a.IsDeath);
            if (chr != null)
            {
                monster.target = chr;
                fsm.ChangeState("chase");
                return;
            }

            //到了需要移动位置的时间
            if (monster.State == Proto.EntityState.Idle)
            {
                //到时间刷新了（每10秒刷新一次）
                if (lastTime + waitTime < Time.time)
                {
                    lastTime = Time.time;
                    waitTime = (float)(param.rand.NextDouble() * 20f) + 10f;
                    //移动到随机位置
                    var target = monster.RandomPointWithBirth(param.walkRange);
                    monster.MoveTo(target);
                }
            }

            //当actor状态不健康的时候回血回蓝
            if (!monster.ActorHealth())
            {
                if (lastRestoreHpMpTime + restoreWaitTime < Time.time)
                {
                    lastRestoreHpMpTime = Time.time;
                    monster.RestoreHealthState();
                }

            }

        }

    }
}
