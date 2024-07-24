using GameServer.Buffs;
using GameServer.Buffs.BuffImplement;
using GameServer.core;
using GameServer.Core;
using GameServer.Model;
using Google.Protobuf.WellKnownTypes;
using Proto;
using Serilog;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat.Skill
{
    // todo 改个名叫SkillStage;
    //技能施法的过程：
    //开始 - 前摇 - 激活 - 结束
    public enum Stage
    {
        None,               //无状态
        Intonate,           //吟唱
        Active,             //已激活
        Colding             //冷却中
    }

    public class Skill
    {
        public SkillDefine Define;          //技能定义
        public Actor Owner;                 //技能归属者
        public float ColdDown;              //冷却倒计时，0表示技能可用
        private float RunTime;              //技能运行时间
        public Stage State;                 //当前技能状态
        private int notCrit = 0;            //未触发暴击的次数
        //强制触发暴击的次数(理论的暴击次数，如果你是百分之10的暴击率，那么你10次攻击会有一次暴击)（这里给你保底）（+2是宽限一点）
        private int forceCritAfer => (int)(100f / Owner.Attr.final.CRI) + 2;

        /// <summary>
        /// 技能的目标：pos、actor
        /// </summary>
        public SCObject Target { get; private set; }


        /// <summary>
        /// 是不是一个无目标类型
        /// </summary>
        public bool IsNoneTarget
        {
            get => Define.TargetType == "None";
        }

        /// <summary>
        /// 是不是一个单位类型
        /// </summary>
        public bool IsUnitTarget
        {
            get => Define.TargetType == "单位";
        }

        /// <summary>
        /// 是不是一个点目标类型
        /// </summary>
        public bool IsPointTarget
        {
            get => Define.TargetType == "点";
        }


        /// <summary>
        /// 是否是群体伤害
        /// </summary>
        public bool IsGroupAttack => Define.IsGroupAttack;


        /// <summary>
        /// 是不是普通攻击
        /// </summary>
        public bool IsNormal => Define.Type == "普通攻击";

        /// <summary>
        /// 是否是被动技能
        /// </summary>
        public bool IsPassive => Define.Type == "被动技能";


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="skid"></param>
        public Skill(Actor owner, int skid)
        {
            Owner = owner;
            Define = DataManager.Instance.skillDefineDict[skid];
            if (Define.HitDelay.Length == 0)
            {
                Array.Resize(ref Define.HitDelay, 1);
            }
        }

        /// <summary>
        /// 驱动技能运转
        /// </summary>
        public virtual void Update()
        {
            if (State == Stage.None && ColdDown == 0) return;
            RunTime += Time.deltaTime;

            //蓄气=>激活
            if (State == Stage.Intonate && RunTime >= Define.IntonateTime)
            {
                State = Stage.Active;
                OnActive();
            }

            //激活=>冷却
            if (State == Stage.Active)
            {
                if (RunTime >= Define.IntonateTime + Define.Duration)
                {
                    State = Stage.Colding;
                    OnColdDown();
                }
            }

            //冷却=>结束
            if (ColdDown > 0) ColdDown -= Time.deltaTime;
            if (ColdDown < 0) ColdDown = 0;
            if (State == Stage.Colding && ColdDown == 0)
            {
                OnFinish();
            }

        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        /// <param name="sco"></param>
        /// <returns></returns>
        public CastResult CanUse(SCObject sco)
        {
            //持有者状态不正常
            if (Owner.IsDeath || Owner.State == EntityState.Dizzy) return CastResult.TargetError;

            //被动技能
            if (IsPassive)
                return CastResult.IsPassive;
            //MP不足
            else if (Owner.Mp < Define.Cost)
                return CastResult.MpLack;
            //正在进行
            else if (State != Stage.None && State != Stage.Colding)
                return CastResult.Running;
            //冷却中
            else if (ColdDown > 0)
                return CastResult.ColdDown;
            //Entity已经死亡
            else if (Owner.IsDeath)
                return CastResult.EntityDead;
            //目标已死亡
            else if (sco is SCEntity && (sco.RealObj as Actor).IsDeath)
                return CastResult.EntityDead;

            //单位技能&&施法者和目标的距离超过限制
            if (IsUnitTarget)
            {
                var dist = Vector3.Distance(Owner.EntityData.Position, sco.Position);
                if (float.IsNaN(dist) || dist > Define.EffectAreaRadius)
                    return CastResult.OutOfRange;
            }

            //可用
            return CastResult.Success;
        }

        /// <summary>
        /// 使用技能
        /// </summary>
        /// <param name="sco"></param>
        /// <returns></returns>
        public virtual CastResult Use(SCObject sco)
        {
            Target = sco;
            RunTime = 0;
            State = Stage.Intonate;
            OnIntonate();
            return CastResult.Success;
        }

        /// <summary>
        /// 技能吟唱
        /// </summary>
        public virtual void OnIntonate()
        {
            Owner.curentSkill = this;
            //扣除mp
            Owner.SetMP(Owner.Mp - Define.Cost);
        }

        /// <summary>
        /// 技能激活
        /// </summary>
        public virtual void OnActive()
        {

            ColdDown = Define.CD;//此时真正进入冷却

            //是否有投射物
            if (Define.IsMissile)
            {
                var missile = Missile.Create(this, Owner.Position, Target);
                Owner.currentSpace.fightManager.missiles.Add(missile);
            }
            else
            {
                //Log.Information("Def.HitDelay.Length=" + Define.HitDelay.Length);
                for (int i = 0; i < Define.HitDelay.Length; i++)
                {
                    Scheduler.Instance.AddTask(_hitTrigger, Define.HitDelay[i], 1);
                }
            }
        }

        /// <summary>
        /// 技能冷却
        /// </summary>
        private void OnColdDown()
        {
            //Log.Information("技能后摇完成:" + Define.Name);
            //结束后摇阶段了
            Owner.curentSkill = null;
        }

        /// <summary>
        /// 技能的生命周期结束
        /// </summary>
        public virtual void OnFinish()
        {
            RunTime = 0;
            State = Stage.None;
        }

        /// <summary>
        /// 触发技能的延迟伤害
        /// 这个就是技能进入active状态后的hitdelay秒后触发伤害
        /// </summary>
        private void _hitTrigger()
        {
            OnHit(Target);
        }

        /// <summary>
        /// 攻击技能范围内的敌军
        /// </summary>
        /// <param name="sco"></param>
        public virtual void OnHit(SCObject targetSco)
        {
            //区分skill的攻击类型是什么？
            //1.单体、点、none
            //2.群攻，单体伤害
            //单体目标的情况下，target就是敌人了，但是要从我们自身出发开始检测
            //在无目标的情况下，targetSco一般是指向自己的
            //在点目标的情况下，targetSco一般是一个位置

            if (IsUnitTarget)
            {
                //群攻型技能，
                if (Define.IsGroupAttack)
                {
                    var sce = new SCEntity(Owner);
                    OnHitGroup(sce);
                }
                //单体技能
                else
                {
                    OnHitOne(targetSco.RealObj as Actor);
                }

            }
            else if (IsPointTarget)
            {
                OnHitGroup(targetSco);
            }
            else if (IsNoneTarget)
            {
                OnHitGroup(targetSco);
            }

        }

        /// <summary>
        /// 由投射物出发的伤害
        /// </summary>
        public virtual void OnHitByMissile(SCObject targetSco)
        {

            //群攻型技能，群攻投射物造成的伤害范围只有圆形
            //target : actor、pos
            if (Define.MissileIsGroupAttack)
            {
                List<Actor> result = new List<Actor>();
                List<Actor> entityList;
                if (targetSco is SCEntity target)
                {
                    var acotr = targetSco.RealObj as Actor;
                    entityList = Owner.currentSpace.AOIManager.GetEntities(acotr.AoiPos).OfType<Actor>().ToList<Actor>();
                }
                else if (targetSco is SCPosition scPos)
                {
                    entityList = (List<Actor>)Owner.currentSpace.AOIManager.GetEntities(scPos.Position.x / 1000, scPos.Position.z / 1000).OfType<Actor>();
                }
                else
                {
                    entityList = new List<Actor>();
                }

                foreach (var entity in entityList)
                {
                    if (entity == Owner) continue;
                    if (CheckForLegalCircularArea(targetSco.Position, entity, Define.MissileEffectRadius))
                    {
                        result.Add(entity);
                    }
                }

                foreach (var item in result)
                {
                    TakeDamage(item);
                }

            }
            //单体技能
            //target : actor
            else
            {
                OnHitOne(targetSco.RealObj as Actor);
            }
        }

        /// <summary>
        /// 单体命中,大部分都是使用扇形区域作为攻击判断是否有效
        /// </summary>
        public virtual void OnHitOne(Actor target)
        {

            bool isLegalArea = CheckForLegalSectorArea(Owner,target, 90, Define.EffectAreaRadius);

            if (isLegalArea)
            {
                TakeDamage(target);
            }

        }

        /// <summary>
        /// 群体命中
        /// </summary>
        public virtual void OnHitGroup(SCObject targetSco)
        {

            var list = GetAttackAreaEntitysNoOwner(Define.EffectAreaType, targetSco);
            foreach (var item in list)
            {
                TakeDamage(item);
            }
        }


        /// <summary>
        /// 对目标造成伤害，计算伤害并且通知目标它被伤害了
        /// </summary>
        /// <param name="acotr"></param>
        private void TakeDamage(Actor targetActor)
        {
            if (targetActor.IsDeath || targetActor == Owner) return;

            //1.计算伤害、闪避、暴击
            //人物的属性
            var attackerAttr = Owner.Attr.final;
            var targetAttr = targetActor.Attr.final;
            //伤害信息
            Damage damage = new Damage();
            damage.AttackerId = Owner.EntityId;
            damage.TargetId = targetActor.EntityId;
            damage.SkillId = Define.ID;
            //技能的物攻和法攻
            //技能本身的攻击力+人物的攻击力*加成百分比
            var ad = Define.AD + attackerAttr.AD * Define.ADC;
            var ap = Define.AP + attackerAttr.AP * Define.APC;
            //计算伤害
            //伤害 = 攻击[攻] × ( 1 - 护甲[守] / ( 护甲[守] + 400 + 85 × 等级[敌人] ) )
            var ads = ad * (1 - targetAttr.DEF / (targetAttr.DEF + 400 + 85 * Owner.Level));
            var aps = ap * (1 - targetAttr.MDEF / (targetAttr.MDEF + 400 + 85 * Owner.Level));
            //Log.Information("ads=[{0}],aps=[{1}]", ads, aps);
            damage.Amount = ads + aps;
            //计算暴击
            notCrit++;
            Random random = new Random();
            double randCri = random.NextDouble();
            double cri = attackerAttr.CRI * 0.01f;
            //Log.Information("暴击率计算：{0}/{1} | [{2}/{3}]", randCri, cri,notCrit,forceCritAfer);
            if (randCri < cri || notCrit > forceCritAfer)
            {
                notCrit = 0;
                damage.IsCrit = true;
                damage.Amount *= attackerAttr.CRD * 0.01f;
            }
            //计算是否命中
            var hitRate = (attackerAttr.HitRate - targetAttr.DodgeRate) * 0.01f;
            //Log.Information("Hit rate : {0}", hitRate);
            if (random.NextDouble() > hitRate)
            {
                damage.IsMiss = true;
                damage.Amount = 0;
            }

            //2.扣除目标Hp
            targetActor.RecvDamage(damage);

            //3.选择是否添加buff
            if (Define.ID == 2002)
            {
                if (!targetActor.IsDeath)
                {
                    targetActor.buffManager.AddBuff<DizzyBuff>(Owner);
                    targetActor.buffManager.AddBuff<BurnBuff>(Owner);
                }

            }


        }


        /// <summary>
        /// 获取攻击区域内的目标们
        /// </summary>
        /// <param name="EffectAreaType"></param>
        /// <param name="targetSco"></param>
        /// <returns></returns>
        private List<Actor> GetAttackAreaEntitysNoOwner(string EffectAreaType, SCObject targetSco)
        {
            List<Actor> result = new List<Actor>();

            //以targetSco 为起始的区域

            List<Actor> entityList;
            if (targetSco is SCEntity target)
            {
                var acotr = targetSco.RealObj as Actor;
                entityList = Owner.currentSpace.AOIManager.GetEntities(acotr.AoiPos).OfType<Actor>().ToList<Actor>();
            }
            else if(targetSco is SCPosition scPos)
            {
                entityList = (List<Actor>)Owner.currentSpace.AOIManager.GetEntities(scPos.Position.x/1000,scPos.Position.z/1000).OfType<Actor>();
            }
            else
            {
                entityList = new List<Actor>();
            }

            if (Define.EffectAreaType == "扇形")
            {
                var acotr = targetSco.RealObj as Actor;
                foreach (var entity in entityList)
                {
                    if (entity == Owner) continue;//忽略技能持有者

                    if (CheckForLegalSectorArea(acotr.Position,acotr.Direction, entity, Define.EffectAreaAngle, Define.EffectAreaRadius))
                    {
                        result.Add(entity);
                    }
                }
            }
            else if (Define.EffectAreaType == "圆形")
            {
                //这里要区分targetSco是actor和一个点的区别
                foreach (var entity in entityList)
                {
                    if (entity == Owner) continue;
                    if (CheckForLegalCircularArea(targetSco.Position, entity, Define.EffectAreaRadius))
                    {
                        result.Add(entity);
                    }
                }
            }
            else if (Define.EffectAreaType == "矩形")
            {

                var acotr = targetSco.RealObj as Actor;
                foreach (var entity in entityList)
                {
                    if (entity == Owner) continue;
                    if (CheckForLegalRectangularArea(acotr.Position, acotr.Direction, entity, Define.EffectAreaLengthWidth[0], Define.EffectAreaLengthWidth[1]))
                    {
                        result.Add(entity);
                    }
                }
            }
            else
            {
                //none
                //有问题
            }

            return result;

        }

        /// <summary>
        /// 检查当前目标是否在合法的扇形区域中
        /// </summary>
        /// <param name="targetActor"></param>
        protected bool CheckForLegalSectorArea(Vector3Int pos,Vector3Int dir, Actor targetActor,  float detectionAngle, float detectionRadius)
        {
            // 将欧拉角转换为弧度
            float yaw = dir.y * Mathf.Deg2Rad *0.001f;

            // 计算当前技能拥有者朝向的单位向量
            Vector3 forwardVector = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));

            // 计算角色到敌人的向量
            Vector3 toEnemy = targetActor.Position - pos;

            // 计算角度差
            float angle = Vector3.Angle(forwardVector, toEnemy);

            // 如果敌人在扇形区域内并且在检测半径范围内
            if (angle <= detectionAngle / 2 && toEnemy.magnitude <= detectionRadius)
            {
                // 在这里可以执行相应的逻辑，比如标记敌人等
                return true;
            }

            return false;
        }
        protected bool CheckForLegalSectorArea(Actor originActor, Actor targetActor, float detectionAngle, float detectionRadius)
        {
            return CheckForLegalSectorArea(originActor.Position, originActor.Direction, targetActor, detectionAngle, detectionRadius);       
        }

        /// <summary>
        /// 检查当前目标是否在合法的圆形区域内
        /// </summary>
        /// <param name="ownerActor"></param>
        /// <param name="targetActor"></param>
        /// <param name="detectionRadius"></param>
        /// <returns></returns>
        protected bool CheckForLegalCircularArea(Vector3Int pos, Actor targetActor, float detectionRadius)
        {
            // 计算角色到敌人的向量
            Vector3 toEnemy = targetActor.Position - pos;

            if(toEnemy.magnitude <= detectionRadius) { return true; }
            return false;
        }

        /// <summary>
        /// 检查当前目标是否在合法的矩形区域内
        /// </summary>
        /// <param name="ownerActor"></param>
        /// <param name="targetActor"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        protected bool CheckForLegalRectangularArea(Vector3Int pos, Vector3Int dir, Actor targetActor, float length, float width) {

            // 计算目标相对于所有者的位置向量
            Vector3 directionToTarget = targetActor.Position - pos;

            // 将欧拉角转换为弧度
            float yaw = dir.y * Mathf.Deg2Rad * 0.001f;

            // 计算角色朝向的单位向量
            Vector3 forwardVector = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));

            // 计算这个向量在所有者面向方向上的投影长度，即目标在所有者前方多远
            float forwardDistance = Vector3.Dot(directionToTarget, forwardVector);

            // 如果目标在所有者背后，或者超出了指定的长度，则不在区域内
            if (forwardDistance < 0 || forwardDistance > length)
            {
                return false;
            }


            // 计算右向单位向量
            Vector3 rightVector = Vector3.Cross(Vector3.up, forwardVector).normalized;

            // 计算目标在所有者右侧的距离，以判断宽度
            float rightDistance = Vector3.Dot(directionToTarget, rightVector);

            // 如果目标相对于中心的距离超出了宽度的一半，则不在区域内
            if (Mathf.Abs(rightDistance) > width / 2)
            {
                return false;
            }

            // 如果以上条件都不满足，则目标在矩形区域内
            return true;
        }

    }
}
