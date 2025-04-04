namespace SceneServer.Core.Combat.AI
{
    public class StateBase
    {
        public virtual void Init(IStateMachineOwner owner)
        {
            // 可能会使用对象池，所以需要每次添加到状态机时进行初始化
        }
        public virtual void UnInit()
        {
            // 反初始化，一般用于释放资源
        }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update(float deltaTime) { }
        public virtual void FixedUpdate() { }
        public virtual void LateUpdate() { }
    }
}
