using GameServer.Combat.Skill;
using GameServer.core.FSM;
using GameServer.Model;
using Proto;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GameServer.AI.State
{
    public class AttackState: IState<Param>
    {
        private Skill curUsedSkill;
        private float waitTime;         //需要等待的时间（技能后摇/攻击失败的惩罚时间）
        private float PunishmentTime;   //攻击的失败惩罚时间

        public AttackState(FSM<Param> fsm)
        {
            this.fsm = fsm;
        }

        public override void OnEnter()
        {
            //先停下来
            var monster = param.owner;
            if (monster.State == EntityState.Motion) {
                monster.StopMove();
            }

            //目标是否有效，无效就返回了
            if(monster.target == null || monster.target.IsDeath)
            {
                monster.target = null;
                fsm.ChangeState("return");
                return;
            }

            //找一个技能，打击目标
            curUsedSkill = monster.Attack(monster.target);

            //攻击失败的惩罚
            if (curUsedSkill == null)
            {
                waitTime = PunishmentTime;
            }
            else
            {
                waitTime = curUsedSkill.Define.PostRockTime;
            }

        }

        public override void OnUpdate()
        {   
            //退出当前状态的条件
            if(waitTime <= 0)
            {
                waitTime = 0;
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
            waitTime -= Time.deltaTime;



        }
    }
}
