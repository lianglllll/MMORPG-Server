using GameServer.core.FSM;
using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameServer.AI.MonsterAI;

namespace GameServer.AI
{
    /// <summary>
    /// AI状态基础类,用于推进状态机
    /// </summary>
    public abstract class AIBase
    {
        public Monster owner;


        public AIBase(Monster owner)
        {
            this.owner = owner;
        }

        public abstract void Update();

    }
}
