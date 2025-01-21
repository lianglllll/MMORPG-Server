using Common.Summer.Core;
using HS.Protobuf.SceneEntity;

namespace GameServer.AI.FSM.State
{
    public class HitState : IState<Param>
    {

        public HitState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
            //如果再移动就先停下来
            var monster = param.owner;
            if (monster.State == ActorState.Move)
            {
                monster.StopMove();
            }
            //看向目标，开始原地罚站

        }

        public override void OnUpdate()
        {
            //退出当前状态的条件
            if (param.remainHitWaitTime <= 0)
            {
                param.remainHitWaitTime = 0;
                var monster = param.owner;
                //如果当前怪物没有死亡，就应该去追击伤害来源的玩家
                if (!monster.IsDeath)
                {
                    if (monster.target != null && !monster.target.IsDeath)
                    {
                        monster.AI.fsm.ChangeState("chase");
                    }
                    else
                    {
                        monster.target = null;
                        monster.AI.fsm.ChangeState("return");
                    }
                }
            }
            param.remainHitWaitTime -= MyTime.deltaTime;
            //Log.Information("[受击后摇傻站]" + param.remainHitWaitTime.ToString());

        }
    }
}
