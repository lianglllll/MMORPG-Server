using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree
{
    public class BehaviorTree
    {
        private Behavior root;//根节点
        
        public bool HaveRoot => root != null;
       
        public BehaviorTree(Behavior root)
        {
            this.root = root;
        }
        
        public void Tick()
        {
            root.Tick();
        }
        
        public void SetRoot(Behavior root)
        {
            this.root = root;
        }
    }
}
