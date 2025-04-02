using SceneServer.Core.Model.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.AI
{
    /// <summary>
    /// AI状态基础类,用于推进状态机
    /// </summary>
    public abstract class AIBase
    {
        public SceneMonster owner;

        public AIBase(SceneMonster owner)
        {
            this.owner = owner;
        }

        public abstract void Update(float deltaTime);

    }
}
