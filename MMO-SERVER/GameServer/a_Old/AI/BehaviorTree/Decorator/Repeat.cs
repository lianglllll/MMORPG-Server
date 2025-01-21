using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Decorator
{
    public class Repeat : BaseDecorator
    {
        private int conunter;//当前重复次数
        private int limit;//重复限度
        public Repeat(int limit)
        {
            this.limit = limit;
        }
        protected override void OnInitialize()
        {
            conunter = 0;//进入时，将次数清零
        }
        protected override EStatus OnUpdate()
        {
            while (true)
            {
                child.Tick();
                if (child.IsRunning)
                    return EStatus.Running;
                if (child.IsFailure)
                    return EStatus.Failure;
                //子节点执行成功，就增加一次计算，达到设定限度才返回成功
                if (++conunter >= limit)
                    return EStatus.Success;
            }
        }
    }
}
