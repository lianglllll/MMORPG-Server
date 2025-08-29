using Common.Summer.Core;
using Common.Summer.Tools;


namespace SceneServer.Core.Combat.Skills
{
    /// <summary>
    /// 投射物
    /// </summary>
    public class Missile
    {
        //对象池
        private static ObjectPool<Missile> _pool = new(() => new Missile());

        //所属技能
        public Skill Skill { get; private set; }
        //追击目标
        public SCObject Target { get; private set; }
        //初始位置
        public Vector3 InitPos { get; private set; }
        //飞行物当前位置
        public Vector3 curPosition;

        public void Init(Skill skill, Vector3 initPos, SCObject target)
        {
            Skill = skill;
            Target = target;
            InitPos = initPos;
            curPosition = initPos;
        }

        public void OnUpdate(float deltaTime)
        {
            var a = curPosition;
            var b = Target.Position;
            Vector3 direction = (b - a).normalized;
            var distance = Skill.Define.MissileSpeed * deltaTime;
            //判断本帧运算是否能到达目标点
            if (distance >= Vector3.Distance(a, b))
            {
                curPosition = b;
                Skill.OnHitByMissile(Target);
                // todo
                // Space.fightManager.missiles.Remove(this);
                _pool.ReturnObject(this);
            }
            else
            {
                curPosition += direction * distance;
            }

        }

        /// <summary>
        /// 创建投射物
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="initPos"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Missile Create(Skill skill, Vector3 initPos, SCObject target)
        {
            var obj = _pool.GetObject();
            obj.Init(skill, initPos, target);
            return obj;
        }


    }

}