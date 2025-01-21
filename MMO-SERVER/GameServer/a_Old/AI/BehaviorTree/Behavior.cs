using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.AI.BehaviorTree
{
    /// <summary>
    /// 运行结果状态的枚举
    /// </summary>
    public enum EStatus
    {
        //失败，成功，运行中，中断，无效
        Failure, Success, Running, Aborted, Invalid
    }

    /// <summary>
    /// 行为树节点基类
    /// </summary>
    public abstract class Behavior
    {
        public bool IsTerminated => IsSuccess || IsFailure;//是否运行结束
        public bool IsSuccess => status == EStatus.Success;//是否成功
        public bool IsFailure => status == EStatus.Failure;//是否失败
        public bool IsRunning => status == EStatus.Running;//是否正在运行
        protected EStatus status;//运行状态
        public Behavior()
        {
            status = EStatus.Invalid;
        }

        //当进入该节点时才会执行一次的函数，类似FSM的OnEnter
        protected virtual void OnInitialize() { }

        //该节点的运行逻辑，会时时返回执行结果的状态，类似FSM的OnUpdate
        protected abstract EStatus OnUpdate();

        //当运行结束时才会执行一次的函数，类似FSM的OnExit
        protected virtual void OnTerminate() { }

        //节点运行，从中应该更能了解上述三个函数的功能
        //它会返回本次调用的结果，为父节点接下来的运行提供依据
        public EStatus Tick()
        {
            if (!IsRunning)
                OnInitialize();

            status = OnUpdate();

            if (!IsRunning)
                OnTerminate();

            return status;
        }

        //添加子节点
        public virtual void AddChild(Behavior child) { }

        //重置该节点的运作
        public void Reset()
        {
            status = EStatus.Invalid;
        }

        //强行打断该节点的运作
        public void Abort()
        {
            OnTerminate();
            status = EStatus.Aborted;
        }
    }

}
