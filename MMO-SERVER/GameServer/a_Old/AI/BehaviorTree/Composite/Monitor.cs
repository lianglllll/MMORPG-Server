using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public class Monitor : CParallel
    {
        public Monitor(Policy mSuccessPolicy, Policy mFailurePolicy)
         : base(mSuccessPolicy, mFailurePolicy)
        {
        }
        public void AddCondition(Behavior condition)
        {
            children.AddFirst(condition);
        }
        public void AddAction(Behavior action)
        {
            children.AddLast(action);
        }
    }
}
