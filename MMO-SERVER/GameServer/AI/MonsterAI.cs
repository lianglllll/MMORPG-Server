using GameServer.AI.State;
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

    //继续拆分ai行为
    //巡逻(idle、motion)  返回(motion)  追击(motion speed)  攻击(skill)   死亡(death)

    //这个就相当于我们客户端的controller脚本，用于控制我们的fsm
    public class MonsterAI : AIBase
    {
        public FSM<Param> fsm;

        public MonsterAI(Monster owner) : base(owner)
        {
            Param param = new Param();
            param.owner = owner;
            fsm = new FSM<Param>(param);

            //添加状态
            fsm.AddState("patrol", new PatrolState(fsm));
            fsm.AddState("chase",new ChaseState(fsm));
            fsm.AddState("return",new ReturnState(fsm));
            fsm.AddState("death",new DeathState(fsm));
            fsm.AddState("skill",new AttackState(fsm));

            //设置初始状态
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
