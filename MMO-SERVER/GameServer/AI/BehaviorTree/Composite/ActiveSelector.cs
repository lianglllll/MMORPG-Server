using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public class ActiveSelector : Selector
    {
        protected override EStatus OnUpdate()
        {
            var prev = currentChild;
            base.OnInitialize();//注意这里，currentChild 会被赋值为 children.First
            var res = base.OnUpdate();//按Selector的OnUpdate执行，顺序遍历选择

            /*
            只要不是遍历结束或可执行节点不变，都应该中断上一次执行的节点，无论优先是高是低。
            因为如果当前优先级比之前的高，理应中断之前的；
            而如果比之前的低，那就证明之前高优先级的行为无法继续了，
            否则怎么会轮到现在的低优先级的行为呢？所以也应中断它。
            */
            if (prev != children.Last && currentChild != prev)
                prev.Value.Abort();
            return res;
        }
    }
}
