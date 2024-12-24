using Common.Summer.Core;
using GameServer.Model;

namespace GameServer.Combat
{
    /// <summary>
    /// Server-Client-Object,用于代理（一个人和一个坐标）
    /// </summary>
    public abstract class SCObject
    {
        protected object realObj;
        public SCObject(object realobj)
        {
            this.realObj = realobj;
        }

        public int Id => GetId();
        public object RealObj => GetRealObj();
        public Vector3 Position => GetPosition();
        public Vector3 Direction => GetDirection();


        protected virtual int GetId() => 0;
        protected virtual object GetRealObj() => realObj;
        protected virtual Vector3 GetPosition() => Vector3.zero;
        protected virtual Vector3 GetDirection() => Vector3.zero;

    }

    //定义SCEntity类,继承自SCObject
    public class SCEntity : SCObject
    {
        private Entity Obj { get => (Entity)realObj; }
        public SCEntity(Entity realobj) : base(realobj)
        {
        }

        protected override int GetId() => Obj.EntityId;
        protected override Vector3 GetDirection() => Obj.Direction;
        protected override Vector3 GetPosition() => Obj.Position;

    }

    public class SCPosition : SCObject
    {
        public SCPosition(Vector3 realobj) : base(realobj)
        {
        }
        protected override Vector3 GetPosition() => (Vector3)realObj;

    }

}
