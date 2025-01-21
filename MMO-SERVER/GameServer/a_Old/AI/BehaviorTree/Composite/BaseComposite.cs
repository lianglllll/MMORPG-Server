using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public abstract class BaseComposite : Behavior
    {
        protected LinkedList<Behavior> children;//用双向链表构建子节点列表

        public BaseComposite()
        {
            children = new LinkedList<Behavior>();
        }

        //移除指定子节点
        public virtual void RemoveChild(Behavior child)
        {
            children.Remove(child);
        }

        public void ClearChildren()//清空子节点列表
        {
            children.Clear();
        }

        public override void AddChild(Behavior child)//添加子节点
        {
            //默认是尾插入，如：0插入「1，2，3」中，就会变成「1，2，3，0」
            children.AddLast(child);
        }

    }
}
