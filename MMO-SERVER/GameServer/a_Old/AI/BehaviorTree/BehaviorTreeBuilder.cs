using GameServer.AI.BehaviorTree.Composite;
using GameServer.AI.BehaviorTree.Decorator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree
{
    public partial class BehaviorTreeBuilder
    {
        private readonly BehaviorTree bhTree;
        private readonly Stack<Behavior> nodeStack;
       
        public BehaviorTreeBuilder()
        {
            bhTree = new BehaviorTree(null);
            nodeStack = new Stack<Behavior>();
        }
       
        private void AddBehavior(Behavior behavior)
        {
            if (bhTree.HaveRoot)
            {
                nodeStack.Peek().AddChild(behavior);//成为栈顶的孩子
            }
            else
            {
                bhTree.SetRoot(behavior);
            }

            //如果不是行为就压栈
            if (behavior is BaseComposite || behavior is BaseDecorator)
            {
                nodeStack.Push(behavior);
            }
        }

        public void TreeTick()
        {
            bhTree.Tick();
        }

        public BehaviorTreeBuilder Back()
        {
            nodeStack.Pop();
            return this;
        }

        public BehaviorTree End()
        {
            nodeStack.Clear();
            return bhTree;
        }

        //---------包装各节点---------
        public BehaviorTreeBuilder Sequence()
        {
            var tp = new Sequence();
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder Seletctor()
        {
            var tp = new Selector();
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder Filter()
        {
            var tp = new Filter();
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder Parallel(CParallel.Policy success, CParallel.Policy failure)
        {
            var tp = new CParallel(success, failure);
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder Monitor(CParallel.Policy success, CParallel.Policy failure)
        {
            var tp = new Monitor(success, failure);
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder ActiveSelector()
        {
            var tp = new ActiveSelector();
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder Repeat(int limit)
        {
            var tp = new Repeat(limit);
            AddBehavior(tp);
            return this;
        }
        public BehaviorTreeBuilder Inverter()
        {
            var tp = new Inverter();
            AddBehavior(tp);
            return this;
        }


    }

    /*
    public class Test
    {
        public Test() { }

        public static void Main(string[] args)
        {
            var builder = new BehaviorTreeBuilder();
            builder.Repeat(3)
                        .Sequence()
                            .DebugNode("Ok,")//由于动作节点不进栈，所以不用Back
                            .DebugNode("It's ")
                            .DebugNode("My time")
                        .Back()
                    .End();
            builder.TreeTick();
        }
    }
    */
}
