using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public class Filter : Sequence
    {
        public void AddCondition(Behavior condition)//添加条件，就用头插入
        {
            children.AddFirst(condition);
        }
        public void AddAction(Behavior action)//添加动作，就用尾插入
        {
            children.AddLast(action);
        }
    }
}
