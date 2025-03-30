using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.AI;
using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Model.Actor
{
    public class SceneMonster :SceneActor
    {
        public MonsterAI m_AI;                  // 怪物Ai(灵魂所在)
        public Vector3 m_initPosition;          // 出生点

        public SceneActor m_target;               // 追击的目标
        public Vector3 m_targetPos;               // 移动时的目标位置
        public Vector3 m_curPos;                  // 移动时的当前位置

        private static Vector3Int Y1000 = new Vector3Int(0, 1000, 0);   // Y单位方向
        private Random random = new Random();                                   // 随机数生成器

        public bool HaveTarget()
        {
            if (m_target == null || m_target.IsDeath ||SceneEntityManager.Instance.GetSceneEntityById(m_target.EntityId) != null)
            {
                m_target = null;
                return false;
            }
            return true;
        }

        public void Init(int professionId, int level, Vector3Int initPos, Vector3Int dir)
        {
            base.Init(initPos, professionId, level);
            m_initPosition = initPos;

            // 补充网络信息
            m_netActorNode.NetActorType = NetActorType.Monster;

            // 给monster注入灵魂
            switch (m_define.AI)
            {
                case "Monster":
                    m_AI = new MonsterAI(this);
                    break;
                default:
                    break;
            }

        }
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            //特殊状态就不需要推动状态机了，因为我们限制没有处理死亡和眩晕的状态
            //如果后面添加了这两个状态就可以去掉这两个校验了
            if (IsDeath) return;
            if (NetActorState == NetActorState.Dizzy) return;

            //推动AI状态跃迁
            m_AI?.Update(deltaTime);

            //推动ai的移动
            MoveToCloseTarget(deltaTime);
        }
        private void MonsterChangeState(NetActorState state)
        {
            if (state == NetActorState) return;
            ActorChangeStateRequest message = new();
            message.EntityId = m_entityId;
            message.State = state;
            message.OriginalTransform = GetTransform();
            // message.Timestamp = ;
            SceneManager.Instance.ActorChangeState(this, message);
        }

        public Skill Attack(SceneActor target)
        {
            Skill result = null;
            // 目标死亡，丢弃本次请求
            if (target == null || target.IsDeath)
            {
                target = null;
                goto End;
            }

            // 拿一个普通攻击来放,目标技能
            var skill = m_skillManager.Skills.FirstOrDefault(s => s.curSkillState == SkillStage.None && s.IsNormalAttack);
            if (skill == null)
            {
                goto End;
            }

            // 看向敌人
            LookAtTarget();

            // 施放技能
            m_skillSpell.RunCast(new CastInfo { CasterId = EntityId, TargetId = target.EntityId, SkillId = skill.Define.ID });
        End:
            return result;
        }
        public void LookAtTarget()
        {
            if (m_target == null)
            {
                goto End;
            }

            Vector3 curV = Position;
            Vector3 targetV = m_target.Position;
            var dir = (targetV - curV).normalized;
            Rotation = LookRotation(dir) * Y1000;

        End:
            return;
        }
        private void MoveToCloseTarget(float deltaTime)
        {
            if (NetActorState == NetActorState.Motion)
            {
                //移动向量
                var dir = (m_targetPos - m_curPos).normalized;
                Rotation = LookRotation(dir) * Y1000;
                float dist = Speed * deltaTime;
                if (Vector3.Distance(m_targetPos, m_curPos) < dist)
                {
                    StopMove();
                }
                else
                {
                    m_curPos += dist * dir;
                }
                this.Position = m_curPos;

                // 广播消息
                ActorChangeTransformDataRequest message = new();
                // message.Timestamp = MyTime.time;
                message.EntityId = EntityId;
                message.OriginalTransform = GetTransform();
                message.PayLoad = new ActorChangeTransformDataPayLoad
                {
                    VerticalSpeed = Speed,
                };
                SceneManager.Instance.ActorChangeTransformData(this, message);
            }
        }
        public void StartMoveToPoint(Vector3 targetPos)
        {
            // 如果处于monster处于特殊状态则不允许移动
            if (NetActorState == NetActorState.Dizzy || NetActorState == NetActorState.Death)
            {
                goto End;
            }

            // 设置monster的状态
            if (NetActorState != NetActorState.Motion)
            {
                MonsterChangeState(NetActorState.Motion);
            }

            //设置monster的目标坐标和当前坐标
            if (m_targetPos != targetPos)
            {
                m_targetPos = targetPos;
                m_curPos = Position;

                var dir = (this.m_targetPos - m_curPos).normalized;//计算方向
                Rotation = LookRotation(dir) * Y1000;

                // 广播消息
                ActorChangeTransformDataRequest message = new();
                message.EntityId = EntityId;
                // message.Timestamp = MyTime.time;
                message.OriginalTransform = GetTransform();
                message.PayLoad = new ActorChangeTransformDataPayLoad
                {
                    VerticalSpeed = Speed,
                };
                SceneManager.Instance.ActorChangeTransformData(this, message);
            }
        End:
            return;
        }
        public void StopMove()
        {
            MonsterChangeState(NetActorState.Idle);
            m_curPos = m_targetPos;
        }

        public override void Revive()
        {
            if (!IsDeath) return;
            ChangeHP(MaxHP);
            ChangeMP(MaxMP);
            
            // 设置当前怪物的位置
            Position = m_initPosition;

            // 设置当前怪物的状态
            MonsterChangeState(NetActorState.Idle);
            ReviveAfter();
        }
        protected override void RecvDamageAfter(Damage damage)
        {
            base.RecvDamageAfter(damage);

            if (IsDeath) return;

            //标记伤害来源为我们的target
            m_target = SceneEntityManager.Instance.GetSceneEntityById(damage.AttackerId) as SceneActor;

            //切换为hit状态
            //设置/重置 受击时间
            if (m_AI.fsm.curStateId != "return")
            {
                LookAtTarget();
                m_AI.fsm.m_param.remainHitWaitTime = m_AI.fsm.m_param.hitWaitTime;
                m_AI.fsm.ChangeState("hit");
            }
        }
        protected override void DeathAfter(int killerID)
        {
            base.DeathAfter(killerID);
            //状态机切换
            m_AI.fsm.ChangeState("death");

            //怪物死亡，得给击杀者奖励啥的：exp
            var killer = SceneEntityManager.Instance.GetSceneEntityById(killerID);
            if (killer != null && killer is SceneCharacter chr)
            {
                // 爆经验
                chr.AddExp(m_define.ExpReward);

                // 爆点装备，直接生成在场景中即可
                // TODO
            }
        }
        protected override void ReviveAfter()
        {
            //状态机切换
            m_AI.fsm.ChangeState("patrol");
        }

        public  bool Check_HpAndMp_Needs()
        {
            if (CurHP < MaxHP || CurMP < MaxMP)
            {
                return true;
            }
            return false;
        }
        public  void Restore_HpAndMp()
        {
            ChangeHP((int)(MaxHP * 0.1f));
            ChangeMP((int)(MaxMP * 0.1f));
        }

        #region Tools

        // 计算出生点附近的随机坐标
        public Vector3 RandomPointWithBirth(float range)
        {
            double x = random.NextDouble() * 2f - 1f;//[-1,1]
            double z = random.NextDouble() * 2f - 1f;
            Vector3 dir = new Vector3(((float)x), 0, ((float)z)).normalized;
            return m_initPosition + dir * range * ((float)random.NextDouble());
        }

        // 方向向量转换位欧拉角
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

        #endregion
    }
}
