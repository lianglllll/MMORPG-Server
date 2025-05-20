using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.AI;
using SceneServer.Core.Combat.AI.MonsterAIStateImpl;
using SceneServer.Core.Combat.AI.WoodenDummy;
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
        public BaseMonsterAI    m_AI;               // 怪物Ai(灵魂所在)
        public Vector3          m_initPosition;     // 出生点
        private Spawner         m_spawner;

        public SceneActor   m_moveToTarget;         // 追击的目标
        public Vector3      m_targetPos;            // 移动时的目标位置
        public Vector3      m_curPos;               // 移动时的当前位置
        private bool        m_isMoving;

        private static Vector3Int Y1000 = new Vector3Int(0, 1000, 0);   // Y单位方向
        private Random random = new Random();                                   // 随机数生成器

        #region getSet
        public bool IsMoving => m_isMoving;
        #endregion

        #region 生命周期函数
        public void Init(int professionId, int level, Spawner spawner)
        {
            m_spawner = spawner;
            Vector3 initPos = spawner.SpawnPoint;
            int offsetX = random.Next(-8, 8);
            int offsetZ = random.Next(-8, 8);
            Vector3 randomizedSpawnPoint = new() 
            {
                x = initPos.x + offsetX * 1000,
                y = initPos.y,
                z = initPos.z + offsetZ * 1000,
            };

            base.Init(randomizedSpawnPoint, professionId, level, null);
            m_initPosition = randomizedSpawnPoint;

            // 补充网络信息
            m_netActorNode.NetActorType = NetActorType.Monster;

        }
        public void Init2()
        {
            // 给monster注入灵魂
            switch (m_spawner.AIName)
            {
                case "Monster":
                    m_AI = new BossMonsterAI(this, m_spawner.PatrolPath);
                    break;
                case "WoodenDummy":
                    m_AI = new WoodenDummyMonsterAI(this);
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
            ChangeActorStateAndSend(NetActorState.Idle);
            ReviveAfter();
        }
        protected override void ReviveAfter()
        {
            // 状态机切换
            m_AI.ChangeState(MonsterAIState.None);
        }
        protected override void RecvDamageAfter(Damage damage)
        {
            if (IsDeath) return;

            // 标记伤害来源为我们的target
            m_moveToTarget = SceneEntityManager.Instance.GetSceneEntityById(damage.AttackerId) as SceneActor;

            // 切换为hit状态
            LookAtTarget(m_moveToTarget);
            m_AI.ChangeState(MonsterAIState.Hurt, true);

            m_netActorNode.NetActorState = NetActorState.Hurt;
            // todo 这个状态可能需要我们自己去控制，而不是让客户端自己去表现。
        }
        protected override void Death(int killerID)
        {
            base.Death(killerID);

            ChangeActorStateAndSend(NetActorState.Death);

            // 状态机切换
            m_AI.ChangeState(MonsterAIState.Death);

            DeathAfter(killerID);
        }
        protected override void DeathAfter(int killerID)
        {
            base.DeathAfter(killerID);

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

        public void ChangeActorStateAndSend(NetActorState state)
        {
            if (state == NetActorState) return;

            m_netActorNode.NetActorState = state;

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
            /*            float Rad2Deg = 57.29578f;
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

                        return eulerAngles;*/

            // 处理零向量
            if (fromDir == Vector3.Zero)
                return Vector3.Zero;

            // 计算俯仰角（Pitch，绕X轴）
            float pitch = MathF.Atan2(fromDir.y, MathF.Sqrt(fromDir.x * fromDir.x + fromDir.x * fromDir.x));
            float pitchDeg = pitch * (180 / MathF.PI); // 弧度转角度

            // 计算偏航角（Yaw，绕Y轴）
            float yaw = MathF.Atan2(fromDir.x, fromDir.z) * (180 / MathF.PI);
            yaw = (yaw < 0) ? yaw + 360 : yaw; // 确保角度在0~360度

            return new Vector3(pitchDeg, yaw, 0);

        }
        public void LookAtTarget(SceneActor target)
        {
            if (target == null)
            {
                goto End;
            }

            // 将当前位置和目标位置的 Y 值置零，强制投影到 XZ 平面
            Vector3 curPosXZ = new Vector3(Position.x, 0, Position.z);
            Vector3 targetPosXZ = new Vector3(target.Position.x, 0, target.Position.z);

            // 计算 XZ 平面方向向量
            Vector3 dir = targetPosXZ - curPosXZ;
            if (dir == Vector3.Zero)
            {
                // 避免零向量
                goto End;
            }
            dir = Vector3.Normalize(dir);
            
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
            LookAtTarget(target);

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
                ChangeActorStateAndSend(NetActorState.Motion);
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

            ChangeActorStateAndSend(NetActorState.Idle);
        End:
            return;
        }
        #endregion
    }
}
