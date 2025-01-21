using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree
{
    public class DebugNode : Behavior
    {
        private string word;
        public DebugNode(string word)
        {
            this.word = word;
        }
        protected override EStatus OnUpdate()
        {
            Console.WriteLine(word);
            return EStatus.Success;
        }
    }

    public partial class BehaviorTreeBuilder
    {
        public BehaviorTreeBuilder DebugNode(string word)
        {
            var node = new DebugNode(word);
            AddBehavior(node);
            return this;
        }
    }

}
