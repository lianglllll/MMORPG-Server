using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public class Selector : Sequence
    {
        protected override EStatus OnUpdate()
        {
            while (true)
            {
                var s = currentChild.Value.Tick();
                if (s != EStatus.Failure)
                    return s;
                currentChild = currentChild.Next;
                if (currentChild == null)
                    return EStatus.Failure;
            }
        }

    }
}
