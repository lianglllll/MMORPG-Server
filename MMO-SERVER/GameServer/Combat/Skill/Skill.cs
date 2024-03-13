using GameServer.Buffs;
using GameServer.Buffs.BuffImplement;
using GameServer.core;
using GameServer.Core;
using GameServer.Model;
using Proto;
using Serilog;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
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
        PostRock,           //后摇
        Colding             //冷却中
    }

    public class Skill
    {
        public SkillDefine Define;          //技能定义
        public Actor Owner;                 //技能归属者
        public float ColdDown;              //冷却倒计时，0表示技能可用
        private float RunTime;              //技能运行时间
        private float PostRockTime;         //后摇时间
        public Stage State;                 //当前技能状态
        public bool IsPassive;              //是否是被动技能
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
        /// 是不是普通攻击
        /// </summary>
        public bool IsNormal => Define.Type == "普通攻击";

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
                ColdDown = Define.CD;//此时真正进入冷却
                OnActive();
            }

            //激活=>后摇
            if (State == Stage.Active)
            {
                if (RunTime >= Define.IntonateTime + Define.HitDelay.Max())
                {
                    State = Stage.PostRock;
                    PostRockTime = Define.PostRockTime;
                    OnPostRock();
                }
            }

            //后摇=>冷却
            if (PostRockTime > 0) PostRockTime -= Time.deltaTime;
            if (PostRockTime < 0) PostRockTime = 0;
            if (State == Stage.PostRock && PostRockTime == 0)
            {
                State = Stage.Colding;
                OnColdDown();
            }


            //冷却
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
            else if (Owner.info.Mp < Define.Cost)
                return CastResult.MpLack;
            //正在进行
            else if (State != Stage.None)
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
            //施法者和目标的距离超过限制
            var dist = Vector3.Distance(Owner.EntityData.Position, sco.Position);
            if (float.IsNaN(dist) || dist > Define.SpellRange)
                return CastResult.OutOfRange;
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
            //Log.Information("Skill Active Owner[{0}],skill[{1}]", Owner.EntityId,Define.Name);
            //Log.Information("技能激活：" + Define.Name);

            //如果是投射物
            if (Define.IsMissile)
            {
                var missile = Missile.Create(this, Owner.Position, Target);
                Owner.currentSpace.fightManager.missiles.Add(missile);
            }
            //如果不是投射物，
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
        /// 技能后摇
        /// </summary>
        public virtual void OnPostRock()
        {

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
            //Log.Information("技能结束：Owner[{0}],skill[{1}]",Owner.EntityId, Define.Name);
        }

        /// <summary>
        /// 触发打到目标的延迟伤害
        /// 这个就是技能进入active状态后的hitdelay秒后触发伤害
        /// </summary>
        private void _hitTrigger()
        {
            //这里还是需要做一些actor和target直接的距离运算再觉得触不触发的
            var dist = Vector3.Distance(Owner.EntityData.Position, Target.Position);
            if (float.IsNaN(dist) || dist <= Define.SpellRange)
            {
                OnHit(Target);
            }
        }

        /// <summary>
        /// 技能打到目标
        /// </summary>
        /// <param name="sco"></param>
        public virtual void OnHit(SCObject targetSco)
        {

            //单体伤害
            if (Define.Area == 0)
            {
                if (targetSco is SCEntity)
                {
                    var acotr = targetSco.RealObj as Actor;
                    TakeDamage(acotr);
                }
            }
            //范围伤害
            else
            {
                var list = GameTools.RangActor(Owner.SpaceId, targetSco.Position, Define.Area);
                foreach (var item in list)
                {
                    TakeDamage(item);
                }
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
            var ads = ad * (1 - targetAttr.DEF / (targetAttr.DEF + 400 + 85 * Owner.info.Level));
            var aps = ap * (1 - targetAttr.MDEF / (targetAttr.MDEF + 400 + 85 * Owner.info.Level));
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
        /// 技能的附加效果
        /// </summary>
        private void AdditionalEffects(Actor targetActor)
        {
            //buff
            var buffs = Define.BUFF;
            if (buffs.Count() <= 0) return;
            foreach (var item in buffs)
            {
                var buffDef = DataManager.Instance.buffDefindeDict[item];
                if (buffDef == null) continue;

                string className = "BurnBuff";
                Type classType = Type.GetType(className);
                if (classType == null) continue;
                BuffBase buff = Activator.CreateInstance(classType) as BuffBase;



            }
        }

    }
}
