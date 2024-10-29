using GameServer.Combat;
using GameServer.Model;
using Proto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Summer.GameServer;
using GameServer.AI.FSM;

namespace GameServer.AI.FSM.State
{

    public class AttackState : IState<Param>
    {
        private Skill curUsedSkill;
        private float afterWaitTime;


        public AttackState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
            //先停下来
            var monster = param.owner;
            if (monster.State == ActorState.Move)
            {
                monster.StopMove();
            }

            //目标是否有效，无效就返回了
            if (monster.target == null || monster.target.IsDeath)
            {
                monster.target = null;
                fsm.ChangeState("return");
                return;
            }

            //找一个技能，打击目标
            curUsedSkill = param.owner.Attack(param.owner.target);

            //攻击失败的惩罚和攻击后摇时间
            if (curUsedSkill == null)
            {
                afterWaitTime = 3f;
            }
            else
            {
                afterWaitTime = curUsedSkill.Define.PostRockTime;
            }

        }

        public override void OnUpdate()
        {

            //技能释放后的后摇时间，这里傻站着不动
            if (afterWaitTime <= 0)
            {
                afterWaitTime = 0;
                var monster = param.owner;
                if (monster.target != null && !monster.target.IsDeath)
                {
                    fsm.ChangeState("chase");
                    return;
                }
                else
                {
                    fsm.ChangeState("return");
                    return;
                }
            }
            afterWaitTime -= MyTime.deltaTime;

        }
    }
}
