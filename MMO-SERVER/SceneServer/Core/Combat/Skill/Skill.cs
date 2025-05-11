using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.SceneEntity;
using SceneServer.Combat.Skills;
using SceneServer.Core.Model.Actor;
using SceneServer.Utils;

namespace SceneServer.Core.Combat.Skills
{
    //技能施法的过程：
    //开始 - 前摇 - 激活 - 结束
    public enum SkillStage
    {
        None,               // 无状态
        Intonate,           // 吟唱
        Active,             // 已激活
        Colding             // 冷却中
    }

    public class Skill
    {
        public SkillDefine Define;          // 技能定义
        public SceneActor Owner;            // 技能归属者
        public float ColdDown;              // 冷却倒计时，0表示技能可用
        private float RunTime;              // 技能运行时间
        public SkillStage curSkillState;    // 当前技能状态
        private int notCriCount = 0;        // 未触发暴击的次数
        private int curHitCnt;
        private int forceCriCount => (int)(100f / Owner.CurAttrubuteDate.CRI) + 2;//强制触发暴击的次数(理论的暴击次数，如果你是百分之10的暴击率，那么你10次攻击会有一次暴击)（这里给你保底）（+2是宽限一点）
        public int SkillId => Define.ID;
        public SCObject Target { get; private set; }        //技能的目标：pos、actor

        public bool IsNoTarget => Define.TargetType == "None";
        public bool IsTarget =>  Define.TargetType == "单位";
        public bool IsPointTarget => Define.TargetType == "点";
        public bool IsNormalAttack => Define.Type == "普通攻击";
        public bool IsPassive => Define.Type == "被动技能";
        public bool IsCanSwitchSkill()
        {
            if (RunTime >= Define.CanSwitchSkillTimePoint) return true;
            return false;
        }

        public Skill(SceneActor owner, int skid)
        {
            Owner = owner;
            Define = StaticDataManager.Instance.skillDefineDict[skid];
            if (Define.HitDelay.Length == 0)
            {
                Array.Resize(ref Define.HitDelay, 1);
            }
        }
        public virtual void Update(float deltaTime)
        {
            if (curSkillState == SkillStage.None) return;
            RunTime += deltaTime;

            if (curSkillState == SkillStage.Intonate)
            {
                //蓄气=>激活
                if (RunTime >= Define.IntonateTime)
                {
                    GetBuffOnIntonateEnd();
                    curSkillState = SkillStage.Active;
                    OnActive();
                }
            }
            else if (curSkillState == SkillStage.Active )
            {
                //激活=>冷却
                if (RunTime >= Define.IntonateTime + Define.Duration)
                {
                    GetBuffOnActiveEnd();
                    curSkillState = SkillStage.Colding;
                    ColdDown = Define.CD;//此时真正进入冷却
                    OnColdDown();
                }
                else
                {
                    OnActivePerFrame();
                }
            }
            else if (curSkillState == SkillStage.Colding)
            {
                // 冷却=>结束
                ColdDown -= deltaTime;
                if(ColdDown <= 0)
                {
                    ColdDown = 0;
                    RunTime = 0;
                    curSkillState = SkillStage.None;
                    OnFinish();
                }
            }

        }
        public CastResult CanUse(SCObject sco)
        {
            // 持有者状态不正常
            if (Owner.NetActorState == NetActorState.Death || Owner.NetActorState == NetActorState.Dizzy)
            {
                return CastResult.TargetError;
            }

            // 被动技能
            if (IsPassive)
                return CastResult.IsPassive;
            // MP不足
            else if (Owner.CurMP < Define.Cost)
                return CastResult.MpLack;
            // 正在进行
            else if (curSkillState != SkillStage.None && curSkillState != SkillStage.Colding)
                return CastResult.Running;
            // 冷却中
            else if (ColdDown > 0)
                return CastResult.ColdDown;
            // Entity已经死亡
            else if (Owner.IsDeath)
                return CastResult.EntityDead;
            // 目标已死亡
            else if (sco is SCEntity && (sco.RealObj as SceneActor).IsDeath)
                return CastResult.EntityDead;

            // 单位技能&&施法者和目标的距离超过限制
            if (IsTarget)
            {
                var dist = Vector3.Distance(Owner.Position, sco.Position);
                if (float.IsNaN(dist) || dist > Define.SpellRangeRadius)
                    return CastResult.OutOfRange;
            }

            //可用
            return CastResult.Success;
        }
        public virtual CastResult Use(SCObject sco)
        {
            Target = sco;
            RunTime = 0;
            curSkillState = SkillStage.Intonate;
            OnIntonate();
            return CastResult.Success;
        }
        protected virtual void OnIntonate()
        {
            Owner.CurUseSkill = this;
            //扣除mp
            Owner.ChangeMP(-Define.Cost);
        }
        protected virtual void OnActive()
        {
            curHitCnt = 0;
            // 是否有投射物
            if (Define.IsMissile)
            {
                var missile = Missile.Create(this, Owner.Position, Target);
                // todo
                // Owner.currentSpace.fightManager.missiles.Add(missile);
            }
        }
        protected virtual void OnActivePerFrame()
        {
            if (curHitCnt >= Define.HitDelay.Length) return;
            float curHitTime = Define.HitDelay[curHitCnt];
            if(RunTime >= Define.IntonateTime + curHitTime)
            {
                OnHit(Target);
                curHitCnt++;
            }
        }
        private void OnColdDown()
        {
            //Log.Information("技能后摇完成:" + Define.Name);
            //结束后摇阶段了
            if(Owner.CurUseSkill == this)
            {
                Owner.CurUseSkill = null;
            }
        }
        protected virtual void OnFinish()
        {
            //本技能已经冷却完成了，完成了技能一次释放的生命周期

        }
        public void CancelSkill()
        {
            curSkillState = SkillStage.Colding;
        }

        protected virtual void GetBuffOnIntonateEnd()
        {
            // 默认调用def中的buff
            foreach(var buffId in Define.OnIntonateEndGetBuffs)
            {
                if(buffId == 0) continue;
                Owner.m_buffManager.AddBuff(Owner, buffId);
            }
        }
        protected virtual void GetBuffOnActiveEnd()
        {
            // 默认调用def中的buff
            foreach (var buffId in Define.OnActiveEndGetBuffs)
            {
                if (buffId == 0) continue;
                Owner.m_buffManager.AddBuff(Owner, buffId);
            }
        }

        protected virtual void OnHit(SCObject targetSco)
        {
            // 区分skill的攻击类型是什么？
            // 需要actor目标的技能：比如说一个追踪的火球只打目标一个，一个横斩击需要目标释放但是是范围伤害
            // 需要点目标的技能 && 不需要目标的技能：打某个区域内的敌人

            // 1.获取符合条件的单位们
            List<SceneActor> targets = new();
            if (IsTarget && Define.IsGroupAttack)       // 单体群攻
            {
                var sce = new SCEntity(Owner);
                targets = GetAttackAreaEntitysNoOwner(Define.EffectAreaType, targetSco);

            }
            else if (IsTarget)                         // 单体
            {
                var target = targetSco.RealObj as SceneActor;
                bool isLegalArea = AreaEntitiesFinder.CheckForLegalSectorArea(Owner, target, 90, Define.SpellRangeRadius);
                if (isLegalArea)
                {
                    targets.Add(target);
                }
            }
            else if (IsPointTarget)
            {
                targets = GetAttackAreaEntitysNoOwner(Define.EffectAreaType, targetSco);
            }
            else if (IsNoTarget)
            {
                targets = GetAttackAreaEntitysNoOwner(Define.EffectAreaType, targetSco);
            }

            // 2.对目标造成伤害
            foreach (var item in targets)
            {
                TakeDamage(item);
            }
        }
        public virtual void OnHitByMissile(SCObject targetSco)
        {
            //群攻型技能，群攻投射物造成的伤害范围只有圆形
            List<SceneActor> targets = new();
            var target = targetSco.RealObj as SceneActor;

            if (!Define.MissileIsGroupAttack)
            {
                //bool isLegalArea = AreaEntitiesFinder.CheckForLegalSectorArea(Owner, target, detectionAngle: 90, Define.SpellRangeRadius);
                //if (isLegalArea)
                //{
                //    targets.Add(target);
                //}
            }
            else
            {
                //targets = AreaEntitiesFinder.GetEntitiesInCircleAroundEntity(target, Define.MissileEffectRadius, true).ToList();
            }

            //对目标造成伤害
            foreach (var item in targets)
            {
                TakeDamage(item);
            }

        }
        protected virtual void TakeDamage(SceneActor targetActor)
        {
            if (targetActor.IsDeath || targetActor == Owner) return;

            // 1.计算伤害、闪避、暴击
            // 人物的属性
            var attackerAttr = Owner.CurAttrubuteDate;
            var targetAttr = targetActor.CurAttrubuteDate;
            // 伤害信息
            Damage damage = new Damage();
            damage.AttackerId = Owner.EntityId;
            damage.TargetId = targetActor.EntityId;
            damage.SkillId = Define.ID;
            // 技能的物攻和法攻
            // 技能本身的攻击力+人物的攻击力*加成百分比
            var ad = Define.AD + attackerAttr.AD * Define.ADC;
            var ap = Define.AP + attackerAttr.AP * Define.APC;
            // 计算伤害
            // 伤害 = 攻击[攻] × ( 1 - 护甲[守] / ( 护甲[守] + 400 + 85 × 等级[敌人] ) )
            var ads = ad * (1 - targetAttr.DEF / (targetAttr.DEF + 400 + 85 * Owner.CurLevel));
            var aps = ap * (1 - targetAttr.MDEF / (targetAttr.MDEF + 400 + 85 * Owner.CurLevel));
            // Log.Information("ads=[{0}],aps=[{1}]", ads, aps);
            damage.Amount = ads + aps;
            // 计算暴击
            notCriCount++;
            Random random = new Random();
            double randCri = random.NextDouble();
            double cri = attackerAttr.CRI * 0.01f;
            // Log.Information("暴击率计算：{0}/{1} | [{2}/{3}]", randCri, cri,notCrit,forceCritAfer);
            if (randCri < cri || notCriCount > forceCriCount)
            {
                notCriCount = 0;
                damage.IsCrit = true;
                damage.Amount *= attackerAttr.CRD * 0.01f;
            }
            // 计算是否命中
            var hitRate = (attackerAttr.HitRate - targetAttr.DodgeRate) * 0.01f;
            // Log.Information("Hit rate : {0}", hitRate);
            if (random.NextDouble() > hitRate)
            {
                damage.IsMiss = true;
                damage.Amount = 0;
            }

            // 2.扣除目标Hp
            targetActor.RecvDamage(damage);

            ////3.选择是否添加buff
            //if (Define.ID == 2002)
            //{
            //    if (!targetActor.IsDeath)
            //    {
            //        targetActor.buffManager.AddBuff<DizzyBuff>(Owner);
            //        targetActor.buffManager.AddBuff<BurnBuff>(Owner);
            //    }
            //}
        }


        private List<SceneActor> GetAttackAreaEntitysNoOwner(string EffectAreaType, SCObject targetSco)
        {
            List<SceneActor> result = new();
            if (IsTarget)
            {
                // 然后根据条件进行筛选
                if (Define.EffectAreaType == "扇形")
                {
                    result = AreaEntitiesFinder.GetEntitiesInSectorAroundSceneActor(Owner, Define.EffectAreaAngle, Define.SpellRangeRadius);
                }
                else if (Define.EffectAreaType == "圆形")
                {
                    result = AreaEntitiesFinder.GetEntitiesInCircleAroundSceneActor(Owner, Define.SpellRangeRadius, false).ToList();
                }else if(Define.EffectAreaType == "矩形")
                {
                    throw new NotImplementedException();
                }else
                {
                    throw new NotImplementedException();
                }
            }
            else if (IsNoTarget)
            {
                // 圆形、扇形、矩形
                if (Define.EffectAreaType == "扇形")
                {
                    result = AreaEntitiesFinder.GetEntitiesInSectorAroundSceneActor(Owner, Define.EffectAreaAngle, Define.SpellRangeRadius);
                }
                else if (Define.EffectAreaType == "圆形")
                {
                    result = AreaEntitiesFinder.GetEntitiesInCircleAroundSceneActor(Owner, Define.EffectAreaRadius, false).ToList();
                }
                else if (Define.EffectAreaType == "矩形")
                {
                    result = AreaEntitiesFinder.GetEntitiesInRectangleAroundEntity(Owner, Define.EffectAreaLengthWidth[0] , Define.EffectAreaLengthWidth[1] );
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (IsPointTarget)
            {
                //圆形
                if (Define.EffectAreaType == "圆形")
                {
                    SCPosition scPos = targetSco as SCPosition;

                    result = AreaEntitiesFinder.GetEntitiesInCircleAroundPoint(scPos.Position, Define.SpellRangeRadius);
                }
                else
                {
                    throw new NotImplementedException();

                }

            }

            return result;

        }
    }
}
