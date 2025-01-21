using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Decorator
{
    public abstract class BaseDecorator : Behavior
    {
        protected Behavior child;
        public override void AddChild(Behavior child)
        {
            this.child = child;
        }
    }
}
