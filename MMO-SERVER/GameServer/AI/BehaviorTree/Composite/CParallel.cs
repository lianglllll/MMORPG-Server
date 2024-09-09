using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree.Composite
{
    public class CParallel : BaseComposite
    {
        /// <summary>
        /// Parallel节点成功与失败的要求，是全部成功/失败，还是一个成功/失败
        /// </summary>

        public enum Policy
        {
            RequireOne, RequireAll,
        }

        protected Policy mSuccessPolicy;//成功的标准
        protected Policy mFailurePolicy;//失败的标准


        //构造函数初始化时，会要求给定成功和失败的标准
        public CParallel(Policy mSuccessPolicy, Policy mFailurePolicy)
        {
            this.mSuccessPolicy = mSuccessPolicy;
            this.mFailurePolicy = mFailurePolicy;
        }

        protected override EStatus OnUpdate()
        {
            int successCount = 0, failureCount = 0;//记录执行成功和执行失败的节点数
            var curNode = children.First;//从第一个子节点开始
            var size = children.Count;
            for (int i = 0; i < size; ++i)
            {
                var bh = curNode.Value;
                if (!bh.IsTerminated)//如果该子节点还没运行结束，就运行它
                    bh.Tick();
                
                curNode = curNode.Next;
                
                if (bh.IsSuccess)//该子节点运行结束后，如果运行成功了
                {
                    ++successCount;//成功执行的节点数+1
                                   //如果是「只要有一个」标准的话，那就可以返回结果了
                    if (mSuccessPolicy == Policy.RequireOne)
                        return EStatus.Success;
                }
                if (bh.IsFailure)//该子节点运行失败的情况同理
                {
                    ++failureCount;
                    if (mFailurePolicy == Policy.RequireOne)
                        return EStatus.Failure;
                }
            }

            //如果是「全都」标准的话，就需要比对当前成功/失败个数与总子节点数
            if (mFailurePolicy == Policy.RequireAll && failureCount == size)
                return EStatus.Failure;
            if (mSuccessPolicy == Policy.RequireAll && successCount == size)
                return EStatus.Success;
            return EStatus.Running;
        }


        //结束函数，只要简单地把所有子节点设为「中断」就可以了
        protected override void OnTerminate()
        {
            foreach (var b in children)
            {
                if (b.IsRunning)
                    b.Abort();
            }
        }
    }
}
