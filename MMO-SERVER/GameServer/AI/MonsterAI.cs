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
            public int walkRange = 12000;        //相对于出生点的活动范围
            public int chaseRange = 15000;      //追击范围
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

            float lastTime = Time.time;         //用于重置下次巡逻的位置
            private static float waitTime = 10f;

            public override void OnEnter()
            {
                param.owner.StopMove();
            }

            public override void OnUpdate()
            {
                var monster = param.owner;
                //查询viewRange内的玩家
                var chr = EntityManager.Instance.GetGetNearEntitys(monster.SpaceId, monster.Position, param.viewRange).FirstOrDefault(a => !a.IsDeath);
                if (chr != null)
                {
                    monster.target = chr;
                    fsm.ChangeState("chase");
                    return;
                }

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

                //切换return状态
                if(monster.target == null || monster.target.IsDeath ||
                    !EntityManager.Instance.Exist(monster.target.EntityId))
                {
                    fsm.ChangeState("return");
                    return;
                }


                //自身和出生点的距离
                float brithDistance = Vector3.Distance(monster.initPosition, monster.Position);
                //自身和目标的距离
                float targetDistance = Vector3.Distance(monster.target.Position, monster.Position);

                //退出追击状态，切换为返回状态
                if (brithDistance > param.walkRange || targetDistance > param.chaseRange)
                {
                    fsm.ChangeState("return");
                    return;
                }

                //符合攻击条件
                if(targetDistance < 1500)
                {
                    if(monster.State == Proto.EntityState.Walk)
                    {
                        monster.StopMove();
                    }
                    //Log.Information("发起攻击");
                    monster.Attack(monster.target);
                }
                else
                {
                    monster.MoveTo(monster.target.Position);
                }




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

            }

        }

    }
}
