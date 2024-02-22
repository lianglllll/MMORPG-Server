using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;
using GameServer.Core;
using GameServer.AI;
using GameServer.Manager;
using GameServer.Combat;

namespace GameServer.Model
{
    public class Monster : Actor
    {
         
        public Vector3 targetPos;           //将要要移动的目标位置（tmp）
        public Vector3 curPos;              //当前移动中的位置(tmp)
        public Vector3 initPosition;        //出生点
        public MonsterAI AI;
        private Random random = new Random();

        public Actor target;                //追击的目标
        private static Vector3Int Y1000 = new Vector3Int(0, 1000, 0);




        public Monster(int Tid,int level,Vector3Int position, Vector3Int direction) : base(EntityType.Monster, Tid,level, position, direction)
        {

            //设置专属monster的info




            //任务1 状态初始化
            initPosition = position;//出生点设置
            State = EntityState.Idle;

            //任务2,monster位置同步
            Scheduler.Instance.AddTask(() =>
            {
                if (State != EntityState.Walk) return;
                //广播消息
                NEntitySync nEntitySync = new NEntitySync();
                nEntitySync.Entity = EntityData;
                nEntitySync.State = State;
                this.currentSpace.UpdateEntity(nEntitySync);
            }, 0.15f);

            //任务3，设置AI对象
            switch (Define.AI)
            {
                case "Monster":
                    this.AI = new MonsterAI(this);
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// 移动到某个点
        /// </summary>
        /// <param name="target"></param>
        public void MoveTo(Vector3 target)
        {
            if(this.State == EntityState.Idle)
            {
                State = EntityState.Walk;//这个能触发下面的update
            }
            if(targetPos != target)
            {
                targetPos = target;
                curPos = Position;
                var dir = (targetPos - curPos).normalized;//计算方向
                Direction = LookRotation(dir) * Y1000;
                //广播消息
                NEntitySync nEntitySync = new NEntitySync();
                nEntitySync.Entity = EntityData;
                nEntitySync.State = State;
                this.currentSpace.UpdateEntity(nEntitySync);
            }
        }

        /// <summary>
        /// 主要是计算服务端位移的数据，每秒50次
        /// </summary>
        public override void Update()
        {
            base.Update();      //技能更新
            AI?.Update();

            //monster移动实现
            if(State == EntityState.Walk)
            {
                //移动方向
                var dir = (targetPos - curPos).normalized;
                this.Direction = LookRotation(dir)* Y1000;
                float dist = Speed * Time.deltaTime;
                if (Vector3.Distance(targetPos, curPos) < dist)
                {
                    StopMove();
                }
                else
                {
                    curPos += dist * dir;
                }
                this.Position = curPos;

            }
        }

        /// <summary>
        /// 怪物攻击
        /// </summary>
        /// <param name="target"></param>
        public void Attack(Actor target)
        {
            if (target.IsDeath)
            {
                target = null;
                return;
            }

            //拿一个普通攻击来放
            var skill = skillManager.Skills.FirstOrDefault(s => s.State == Combat.Stage.None && s.IsNormal);
            if (skill == null) return;
            spell.SpellTarget(skill.Define.ID, target.EntityId);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMove()
        {
            State = EntityState.Idle;
            curPos = targetPos;
            //广播消息
            NEntitySync nEntitySync = new NEntitySync();
            nEntitySync.Entity = EntityData;
            nEntitySync.State = State;
            this.currentSpace.UpdateEntity(nEntitySync);
        }

        /// <summary>
        /// 计算出生点附近的随机坐标
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Vector3 RandomPointWithBirth(float range)
        {
            double x = random.NextDouble()*2f-1f;//[-1,1]
            double z = random.NextDouble()*2f- 1f;
            Vector3 dir = new Vector3(((float)x), 0, ((float)z)).normalized;
            return initPosition + dir * range * ((float)random.NextDouble());
        }

        /// <summary>
        /// 方向向量转换位欧拉角
        /// </summary>
        /// <param name="fromDir"></param>
        /// <returns></returns>
        public Vector3 LookRotation(Vector3 fromDir)
        {
            float Rad2Deg = 57.29578f;
            Vector3 eulerAngles = new Vector3();

            // 计算欧拉角X
            eulerAngles.x = MathF.Acos(MathF.Sqrt((fromDir.x * fromDir.x + fromDir.z * fromDir.z) / (fromDir.x * fromDir.x + fromDir.y * fromDir.y + fromDir.z * fromDir.z))) * Rad2Deg;
            if (fromDir.y > 0)
                eulerAngles.x = 360 - eulerAngles.x;

            // 计算欧拉角Y
            eulerAngles.y = MathF.Atan2(fromDir.x, fromDir.z) * Rad2Deg;
            if (eulerAngles.y < 0)
                eulerAngles.y += 180;
            if (fromDir.x < 0)
                eulerAngles.y += 180;

            // 欧拉角Z为0
            eulerAngles.z = 0;

            return eulerAngles;
        }

        /// <summary>
        /// 死亡前处理
        /// </summary>
        /// <param name="killerID"></param>
        protected override void OnBeforeDie(int killerID)
        {
            //状态机切换
            AI.fsm.ChangeState("death");

            //怪物死亡，得给击杀者奖励啥的：exp
            var killer = EntityManager.Instance.GetEntity(killerID);
            if (killer != null && killer is Character chr)
            {
                //爆经验
                chr.SetExp(chr.Exp + Define.ExpReward);
                //也可以爆点金币
                chr.SetGold(chr.Gold + Define.GoldReward);
                //爆点装备，直接生成在场景中即可

            }
        }

        /// <summary>
        /// 复活后处理
        /// </summary>
        /// <param name="killerID"></param>
        protected override void OnAfterRevive()
        {
            //状态机切换
            AI.fsm.ChangeState("walk");
        }

    }
}
 