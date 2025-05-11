using Common.Summer.Core;
using Google.Protobuf;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Common;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Scene;
using SceneServer.Net;
using SceneServer.Utils;

namespace SceneServer.Core.Model.Actor
{
    public class SceneCharacter : SceneActor
    {
        private string m_cId;
        private Session m_session;

        #region GetSet
        public string SessionId => m_session.SesssionId;
        protected long Exp
        {
            get => NetActorNode.Exp;
            set => NetActorNode.Exp = value;
        }
        public String Cid => m_cId;
        #endregion

        public void Init(string sessionId,Connection conn, DBCharacterNode dbChrNode)
        {
            m_cId = dbChrNode.CId;
            m_session = new Session(sessionId, conn);
            m_session.Chr = this;

            var initPos = new NetVector3 { X = dbChrNode.ChrStatus.X, Y = dbChrNode.ChrStatus.Y, Z = dbChrNode.ChrStatus.Z };
            base.Init(initPos, dbChrNode.ProfessionId, dbChrNode.Level);

            // 补充网络信息
            m_netActorNode.ActorName = dbChrNode.ChrName;
            m_netActorNode.Exp = dbChrNode.ChrStatus.Exp;
            m_netActorNode.NetActorType = NetActorType.Character;

            // 处理已经装载的技能
            if(dbChrNode.ChrCombat != null)
            {
                m_netActorNode.FixedSkillGroupInfo = new FixedSkillGroupInfo();
                foreach(var item in dbChrNode.ChrCombat.EquippedSkills)
                {
                    var node = new FixedSkillInfo()
                    {
                        SkillId = item.SkillId,
                        Pos = item.Pos
                    };
                    m_netActorNode.FixedSkillGroupInfo.Skills.Add(node);
                }
                m_skillManager.AddFixedSkills();
            }
            // todo模拟一下
            m_netActorNode.FixedSkillGroupInfo = new FixedSkillGroupInfo();
            m_netActorNode.FixedSkillGroupInfo.Skills.Add(new FixedSkillInfo
            {
                SkillId = 40001,
                Pos = 1
            });
            m_netActorNode.FixedSkillGroupInfo.Skills.Add(new FixedSkillInfo
            {
                SkillId = 40011,
                Pos = 2
            });
            m_netActorNode.FixedSkillGroupInfo.Skills.Add(new FixedSkillInfo
            {
                SkillId = 40012,
                Pos = 3
            });
            m_skillManager.AddFixedSkills();

            // 处理武器对应的技能组
        }
        public void Send(IMessage message)
        {
            m_session.Send(message);
        }
        public void Send(Scene2GateMsg msg)
        {
            msg.SessionId = m_session.SesssionId;
            m_session.Send(msg);
        }

        public void AddExp(long deltaExp)
        {
            if (deltaExp <= 0) return;

            long oldExp = Exp;
            Exp += deltaExp;

            // 判断当前经验是否足够升级
            int deltaLevel = 0;
            while (StaticDataManager.Instance.levelDefineDefineDict.TryGetValue(CurLevel, out var define))
            {
                if (Exp >= define.ExpLimit)
                {
                    deltaLevel++; 
                    Exp -= define.ExpLimit;
                }
                else
                {
                    break;
                }
            }
            if(deltaLevel > 0)
            {
                UpgradeLevel(deltaLevel);
            }

            //发包
            ActorPropertyUpdate po = new()
            {
                EntityId = EntityId,
                PropertyType = ActorPropertyUpdate.Types.PropType.Exp,
                OldValue = new() { LongValue = oldExp },
                NewValue = new() { LongValue = Exp }
            };

            SceneManager.Instance.FightManager.propertyUpdateQueue.Enqueue(po);
        }
        public void UpgradeLevel(int deltaLevel)
        {
            if (deltaLevel <= 0) return;
            int oldLevel = CurLevel;
            CurLevel += deltaLevel;
            ActorPropertyUpdate po = new()
            {
                EntityId = EntityId,
                PropertyType = ActorPropertyUpdate.Types.PropType.Level,
                OldValue = new() { IntValue = oldLevel },
                NewValue = new() { IntValue = CurLevel }
            };

            // 广播通知
            SceneManager.Instance.FightManager.propertyUpdateQueue.Enqueue(po);

            // 属性刷新
            m_attributeManager.Reload(CurLevel);
        }

        protected override void Death(int killerID)
        {
            base.Death(killerID);
            // 将当前角色的状态设置为当前mode的death状态
            // 并且强制同步到客户端
            ForceChangeActor(NetActorState.Death);
        }
        protected override void ForceChangeActor(NetActorState state)
        {
            base.ForceChangeActor(state);
            ChangeActorState(state);
            var resp = new ActorChangeStateResponse();
            resp.SessionId = SessionId;
            resp.SceneId = SceneManager.Instance.SceneId;
            resp.EntityId = EntityId;
            resp.State = state;
            // resp.Timestamp = ;
            resp.OriginalTransform = GetTransform();
            
            Send(resp);
        }
    }
}
