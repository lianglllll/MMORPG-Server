using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.AI;
using SceneServer.Core.Combat.AI.MonsterAIStateImpl;
using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Model.Actor
{
    public class SceneMonster :SceneActor
    {
        public MonsterAI m_AI;                      // 怪物Ai(灵魂所在)
        public Vector3 m_initPosition;              // 出生点

        public SceneActor m_target;                 // 追击的目标
        public Vector3 m_targetPos;                 // 移动时的目标位置
        public Vector3 m_curPos;                    // 移动时的当前位置
        private bool m_isMoving;

        private static Vector3Int Y1000 = new Vector3Int(0, 1000, 0);   // Y单位方向
        private Random random = new Random();                                   // 随机数生成器

        #region getSet
        public bool IsMoving => m_isMoving;
        public bool HaveTarget()
        {
            if (m_target == null || m_target.IsDeath ||SceneEntityManager.Instance.GetSceneEntityById(m_target.EntityId) != null)
            {
                m_target = null;
                return false;
            }
            return true;
        }
        #endregion

        #region 生命周期函数
        public void Init(int professionId, int level, Vector3Int initPos, Vector3Int dir)
        {
            int offsetX = random.Next(-8, 8);
            int offsetY = random.Next(-8, 8);
            Vector3Int randomizedSpawnPoint = new Vector3Int
            {
                x = initPos.x + offsetX * 1000,
                y = initPos.y + offsetY * 1000,
                z = initPos.z
            };

            base.Init(randomizedSpawnPoint, professionId, level);
            m_initPosition = initPos;

            // 补充网络信息
            m_netActorNode.NetActorType = NetActorType.Monster;

        }
        public void Init2()
        {
            // 给monster注入灵魂
            switch (m_define.AI)
            {
                case "Monster":
                    m_AI = new MonsterAI(this);
                    break;
                case "WoodenStake":
                    // m_AI = new WoodenStakeAI(this);
                    break;
                default:
                    break;
            }
        }
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // 推动AI状态跃迁
            m_AI?.Update(deltaTime);
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
        protected override void ReviveAfter()
        {
            // 状态机切换
            m_AI.ChangeState(MonsterState.Patrol);
        }
        protected override void RecvDamageAfter(Damage damage)
        {
            base.RecvDamageAfter(damage);

            if (IsDeath) return;

            // 标记伤害来源为我们的target
            m_target = SceneEntityManager.Instance.GetSceneEntityById(damage.AttackerId) as SceneActor;

            // 切换为hit状态
            LookAtTarget();
            m_AI.ChangeState(MonsterState.Hurt);
        }
        protected override void DeathAfter(int killerID)
        {
            base.DeathAfter(killerID);
            // 状态机切换
            m_AI.ChangeState(MonsterState.Dead);

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
        #endregion

        #region Tools

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

        public bool IsCanAttack()
        {
            return m_skillSpell.IsCanCast();
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

        public void StartMoveToPoint(Vector3 targetPos, int speed)
        {
            if(m_isMoving && targetPos == m_targetPos)
            {
                goto End;
            }
            m_isMoving = true;

            // 设置monster的目标坐标和当前坐标
            m_targetPos = targetPos;
            m_curPos = Position;
            Speed = speed;

            // 方向设置一下
            var dir = (this.m_targetPos - m_curPos).normalized;
            Rotation = LookRotation(dir) * Y1000;

            // 设置monster的状态
            if (NetActorState != NetActorState.Motion)
            {
                MonsterChangeState(NetActorState.Motion);
            }

        End:
            return;
        }
        public void Moving(float deltaTime)
        {
            if (!m_isMoving)
            {
                goto End;
            }



            // 移动向量
            var dir = (m_targetPos - m_curPos).normalized;
            float dist = Speed * deltaTime;
            m_curPos += dist * dir;
            if (Vector3.Distance(m_targetPos, m_curPos) < 100f)
            {
                Position = m_targetPos;
                StopMove();
            }
            else
            {
                Rotation = LookRotation(dir) * Y1000;
                Position = m_curPos;

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

        End:
            return;
        }
        public void StopMove()
        {
            if(!m_isMoving) {
                goto End;
            }
            m_isMoving = false;
            
            m_curPos = m_targetPos;

            MonsterChangeState(NetActorState.Idle);
        End:
            return;
        }

        #endregion
    }
}
