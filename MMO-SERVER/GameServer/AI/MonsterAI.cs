using GameServer.core.FSM;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using Serilog;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI
{

    //继续拆分ai行为
    //巡逻(idle、motion)  返回(motion)  追击(motion speed)  攻击(skill)   死亡(death)

    //这个就相当于我们客户端的controller脚本，用于控制我们的fsm
    public class MonsterAI : AIBase
    {
        /// <summary>
        /// 状态间共享数据
        /// </summary>
        public class Param
        {
            public Monster owner;               //AI拥有者
            public int viewRange = 8000;        //视野范围
            public int walkRange = 20000;       //相对于出生点的活动范围
            public int chaseRange = 30000;      //追击范围
            public Random rand = new Random();
        }

        public FSM<Param> fsm;

        public MonsterAI(Monster owner) : base(owner)
        {
            Param param = new Param();
            param.owner = owner;
            fsm = new FSM<Param>(param);
            //添加状态
            fsm.AddState("walk",new WalkState());
            fsm.AddState("chase",new ChaseState());
            fsm.AddState("return",new ReturnState());
            fsm.AddState("death",new DeathState());

            //设置初始状态
        }

        /// <summary>
        /// 驱动状态机
        /// </summary>
        public override void Update()
        {
            fsm.Update();
        }

        /// <summary>
        /// 巡逻状态
        /// </summary>
        class WalkState : IState<Param>
        {

            float lastTime = Time.time;             //用于重置下次巡逻的位置
            private static float waitTime = 10f;

            float lastRestoreHpMpTime = Time.time;  //用于重置回复状态的时间点
            private static float restoreWaitTime = 1f;

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
                    if(lastTime+ waitTime < Time.time)
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
                    if(lastRestoreHpMpTime + restoreWaitTime < Time.time)
                    {
                        lastRestoreHpMpTime = Time.time;
                        monster.RestoreHealthState();
                    }

                }

            }

        }

        /// <summary>
        /// 追击状态
        /// </summary>
        class ChaseState : IState<Param>
        {
            public override void OnUpdate()
            {
                var monster = param.owner;

                //追击目标失效切换为返回状态
                if(monster.target == null || monster.target.IsDeath ||!EntityManager.Instance.Exist(monster.target.EntityId))
                {
                    monster.target = null;
                    fsm.ChangeState("return");

/*                    if (fsm.curStateId != "death")
                    {
                    }*/
                    return;
                }

                //计算距离
                float brithDistance = Vector3.Distance(monster.initPosition, monster.Position);
                float targetDistance = Vector3.Distance(monster.target.Position, monster.Position);

                //当超过我们的活动范围或者追击范围，切换返回状态
                if (brithDistance > param.walkRange || targetDistance > param.chaseRange)
                {
                    monster.target = null;
                    fsm.ChangeState("return");

/*                    if (fsm.curStateId != "death")
                    {
                    }*/
                    return;
                }

                //攻击距离不够，我们继续靠近目标
                if(targetDistance > 2000)
                {
                    monster.MoveTo(monster.target.Position);
                    return;
                }

                //在技能后摇结束之前，我们不能再次攻击
                if (monster.curentSkill != null) return;

                //到这里就符合攻击条件了
                //符合攻击条件的同时其实也可以同时攻击，也就是说行走和攻击的动画需要混合。
                //后面客户端可能就会使用原本的动画状态机了，然后网络传送传送动画的变量。。
                //因为动画之间的切换好生硬了
                //这里如果切换到idle在攻击，会发送idle和walk抖动
                if (monster.State == Proto.EntityState.Motion)
                {
                    monster.StopMove();
                }

                monster.Attack(monster.target);

            }
        }

        /// <summary>
        /// 返回状态
        /// </summary>
        class ReturnState : IState<Param>
        {

            public override void OnEnter()
            {
                param.owner.MoveTo(param.owner.initPosition);
            }

            public override void OnUpdate()
            {
                var monster = param.owner;


                if(Vector3.Distance(monster.initPosition,monster.Position)< 100)
                {
                    fsm.ChangeState("walk");
                }
            }

        }

        /// <summary>
        /// 死亡状态
        /// </summary>
        class DeathState : IState<Param>
        {

            public override void OnEnter()
            {

            }

            public override void OnUpdate()
            {
                var monster = param.owner;
                if (!monster.IsDeath)
                {
                    fsm.ChangeState("walk");
                }
            }

        }

        /// <summary>
        /// ai受到伤害的回调
        /// </summary>
        /// <param name="actor"></param>
        public void RecvDamageCallBack(Actor actor)
        {
            if (actor == null) return;
            //如果当前怪物没有死亡，就应该去追击攻击本monster的玩家
            if (!fsm.param.owner.IsDeath && fsm.curStateId != "chase" && fsm.curStateId != "return")
            {
                var monster = fsm.param.owner;
                monster.target = actor;
                fsm.ChangeState("chase");
            }
        }

    }
}
