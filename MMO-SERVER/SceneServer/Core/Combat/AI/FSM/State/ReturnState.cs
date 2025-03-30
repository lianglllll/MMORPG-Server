using Common.Summer.Core;
using HS.Protobuf.SceneEntity;

namespace SceneServer.Core.Combat.AI.FSM.State
{
    /// <summary>
    /// 返回状态
    /// </summary>
    public class ReturnState : IState<Param>
    {
        public ReturnState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
            param.owner.StartMoveToPoint(param.owner.m_initPosition);
        }

        public override void OnUpdate()
        {
            var monster = param.owner;

            //返回时被眩晕
            if (monster.NetActorState == NetActorState.Dizzy) return;

            //有问题，我们切换为巡逻状态
            if (monster.NetActorState != NetActorState.Motion)
            {
                fsm.ChangeState("patrol");
                return;
            }

            //接近到出生点就切换为巡逻状态
            if (Vector3.Distance(monster.m_initPosition, monster.Position) < 100)
            {
                fsm.ChangeState("patrol");
            }
        }

    }
}
