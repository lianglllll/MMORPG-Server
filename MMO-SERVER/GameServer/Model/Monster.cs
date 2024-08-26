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
using GameServer.core.FSM;
using GameServer.Combat.Skill;
using Serilog;
using System.Threading;

namespace GameServer.Model
{
    public class Monster : Actor
    {
        public MonsterAI AI;                //怪物Ai(灵魂所在)
        public Vector3 initPosition;        //出生点

        public Actor target;                //追击的目标
        public Vector3 targetPos;           //移动时的目标位置
        public Vector3 curPos;              //移动时的当前位置

        private static Vector3Int Y1000 = new Vector3Int(0, 1000, 0);   //Y单位方向
        private Random random = new Random();                                   //随机数生成器


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Tid"></param>
        /// <param name="level"></param>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        public Monster(int Tid,int level,Vector3Int position, Vector3Int direction) : base(EntityType.Monster, Tid,level, position, direction)
        {

            //初始化
            initPosition = position;    //出生点设置
            State = EntityState.Idle;   //monster状态设置

/*            //添加怪物同步定时任务
            Scheduler.Instance.AddTask(() =>
            {
                //if (IsDeath) return;
                if (State != EntityState.Motion) return;

                //广播消息
                NEntitySync nEntitySync = new NEntitySync();
                nEntitySync.Entity = EntityData;
                nEntitySync.State = State;
                this.currentSpace.UpdateEntity(nEntitySync);
            }, 0.15f);*/


            //给monster注入灵魂
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
        /// 主要是计算服务端位移的数据，每秒50次
        /// </summary>
        public override void Update()
        {
            base.Update();      

            //特殊状态就不需要推动状态机了，因为我们限制没有处理死亡和眩晕的状态
            //如果后面添加了这两个状态就可以去掉这两个校验了
            if (IsDeath) return;
            if (State == EntityState.Dizzy) return;

            //推动AI状态跃迁
            AI?.Update();

            //推动ai的移动
            Move();
        }

        /// <summary>
        /// 怪物攻击
        /// </summary>
        /// <param name="target"></param>
        public Skill Attack(Actor target)
        {
            //目标死亡，丢弃本次请求
            if (target == null || target.IsDeath)
            {
                target = null;
                return null;
            }

            //拿一个普通攻击来放,目标技能
            var skill = skillManager.Skills.FirstOrDefault(s => s.State == Stage.None && s.IsNormal);
            if (skill == null) return null;

            //看向敌人
            LookAtTarget();

            //施放技能
            spell.RunCast(new CastInfo { CasterId = target.EntityId, TargetId = target.EntityId ,SkillId = skill.Define.ID});

            return skill;
        }

        /// <summary>
        /// 看向敌人
        /// </summary>
        public void LookAtTarget()
        {
            if(target == null) return;
            Vector3 curV = Position;
            Vector3 targetV = target.Position;
            var dir = (targetV-curV).normalized;
            Direction = LookRotation(dir) * Y1000;

            //广播消息，主要是广播了monster这个旋转
            NEntitySync nEntitySync = new NEntitySync();
            nEntitySync.Entity = EntityData;
            nEntitySync.State = EntityState.NoneState;
            this.currentSpace.SyncActor(nEntitySync,this);
        }

        /// <summary>
        /// monster移动，驱动monster不断靠近目标点
        /// 当到达目标点时，自动停止移动
        /// </summary>
        private void Move()
        {
            if (State == EntityState.Motion)
            {
                //移动向量
                var dir = (targetPos - curPos).normalized;
                this.Direction = LookRotation(dir) * Y1000;
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

                //广播消息
                NEntitySync nEntitySync = new NEntitySync();
                nEntitySync.Entity = EntityData;
                nEntitySync.State = State;
                this.currentSpace.SyncActor(nEntitySync,this);
            }
        }

        /// <summary>
        /// monstor移动到目标点
        /// </summary>
        /// <param name="targetPos"></param>
        public void StartMoveTo(Vector3 targetPos)
        {
            //如果处于monster处于特殊状态则不允许移动
            if (State == EntityState.Dizzy || State == EntityState.Death)
            {
                return;
            }

            //设置monster的状态
            if(State != EntityState.Motion)
            {
                State = EntityState.Motion;
            }

            //设置monster的目标坐标和当前坐标
            if (this.targetPos != targetPos)
            {
                this.targetPos = targetPos;
                curPos = Position;

                var dir = (this.targetPos - curPos).normalized;//计算方向
                Direction = LookRotation(dir) * Y1000;

                //广播消息，主要是广播了monster这个状态：motion
                NEntitySync nEntitySync = new NEntitySync();
                nEntitySync.Entity = EntityData;
                nEntitySync.State = State;
                this.currentSpace.SyncActor(nEntitySync, this);
            }
        }

        /// <summary>
        /// monster停止移动
        /// </summary>
        public void StopMove()
        {
            State = EntityState.Idle;
            curPos = targetPos;

            //广播消息,广播移动结束时的坐标和monster状态
            NEntitySync nEntitySync = new NEntitySync();
            nEntitySync.Entity = EntityData;
            nEntitySync.State = State;
            this.currentSpace?.SyncActor(nEntitySync,this);
        }

        /// <summary>
        /// 死亡后处理
        /// </summary>
        /// <param name="killerID"></param>
        protected override void OnAfterDie(int killerID)
        {
            base.OnAfterDie(killerID);
            //状态机切换
            AI.fsm.ChangeState("death");

            //怪物死亡，得给击杀者奖励啥的：exp
            var killer = EntityManager.Instance.GetEntityById(killerID);
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
        /// 复活
        /// </summary>
        public override void Revive()
        {
            if (!IsDeath) return;
            SetHp(Attr.final.HPMax);
            SetMP(Attr.final.MPMax);
            SetMacroState(UnitState.Free);
            //设置当前怪物的位置
            Position = initPosition;
            SetEntityState(EntityState.Idle);
            OnAfterRevive();
        }

        /// <summary>
        /// 复活后处理
        /// </summary>
        /// <param name="killerID"></param>
        protected override void OnAfterRevive()
        {
            //状态机切换
            AI.fsm.ChangeState("patrol");
        }

        /// <summary>
        /// 受伤后处理
        /// </summary>
        /// <param name="damage"></param>
        protected override void AfterRecvDamage(Damage damage)
        {
            base.AfterRecvDamage(damage);

            if (IsDeath) return;

            //标记伤害来源为我们的target
            target = EntityManager.Instance.GetEntityById(damage.AttackerId) as Actor;

            //切换为hit状态
            //设置/重置 受击时间
            if(AI.fsm.curStateId != "return")
            {
                LookAtTarget();
                AI.fsm.param.remainHitWaitTime = AI.fsm.param.hitWaitTime;
                AI.fsm.ChangeState("hit");
            }


        }

        /// <summary>
        /// 计算出生点附近的随机坐标
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Vector3 RandomPointWithBirth(float range)
        {
            double x = random.NextDouble() * 2f - 1f;//[-1,1]
            double z = random.NextDouble() * 2f - 1f;
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

    }
}
 