using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public class Sequence : BaseComposite
    {
        protected LinkedListNode<Behavior> currentChild;//当前运行的子节点

        protected override void OnInitialize()
        {
            currentChild = children.First;//从第一个子节点开始
        }

        protected override EStatus OnUpdate()
        {
            while (true)
            {
                var s = currentChild.Value.Tick();//记录子节点运行返回的结果状态

                /*
                如果子节点运行，还没有成功，就直接返回该结果。
                是「运行中」那就表明本节点也是运行中，有记录当前节点，下次还会继续执行；
                是「失败」就表明本节点也运行失败了，下次会再经历OnInitialize，从头开始。
                */
                if (s != EStatus.Success)
                    return s;

                //如果运行成功，就换到下一个子节点
                currentChild = currentChild.Next;

                //如果全都成功运行完了，就返回「成功」
                if (currentChild == null)
                    return EStatus.Success;
            }
        }
    }

}
