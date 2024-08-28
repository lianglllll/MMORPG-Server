using GameServer.AI.State;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using Serilog;
using GameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.AI.FSM;

namespace GameServer.AI
{
    /// <summary>
    /// 行为间共享数据
    /// </summary>
    public class Param
    {
        public Monster owner;               //AI拥有者
        public int viewRange = 8000;        //视野范围
        public int walkRange = 20000;       //相对于出生点的活动范围
        public int chaseRange = 30000;      //追击范围
        public Random rand = new Random();
        public float hitWaitTime = 1.5f;    //受击后摇时间
        public float remainHitWaitTime;     
    }

    /// <summary>
    /// 怪物AI，与其说是状态机，不如说是行为机，这里是基于怪物的行为而不是状态来驱动的
    /// 行为=状态1+状态2+...
    /// </summary>
    public class MonsterAI : AIBase
    {
        public FSM<Param> fsm;

        public MonsterAI(Monster owner) : base(owner)
        {
            Param param = new Param();
            param.owner = owner;
            fsm = new FSM<Param>(param,5);

            //添加行为
            fsm.AddState("patrol", new PatrolState(fsm));
            fsm.AddState("chase",new ChaseState(fsm));
            fsm.AddState("return",new ReturnState(fsm));
            fsm.AddState("death",new DeathState(fsm));
            fsm.AddState("attack",new AttackState(fsm));
            fsm.AddState("hit",new HitState(fsm));

            //设置初始行为
            fsm.ChangeState("patrol");

        }

        /// <summary>
        /// 驱动状态机
        /// </summary>
        public override void Update()
        {
            fsm.Update();
        }

    }

}
