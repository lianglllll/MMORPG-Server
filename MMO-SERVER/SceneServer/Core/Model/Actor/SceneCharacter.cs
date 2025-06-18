using Common.Summer.Core;
using Common.Summer.Tools.GameEvent;
using Google.Protobuf;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Common;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.AOIMap.NineSquareGrid;
using SceneServer.Core.Model.Item;
using SceneServer.Core.Scene;
using SceneServer.Core.Task;
using SceneServer.Net;
using SceneServer.Utils;

namespace SceneServer.Core.Model.Actor
{
    public class SceneCharacter : SceneActor
    {
        private string m_cId;
        private Session m_session;
        private CharacterEventSystem m_characterEventSystem;
        private GameTaskConditionChecker m_gameTaskConditionChecker;

        #region GetSet
        public string SessionId => m_session.SesssionId;
        protected long Exp
        {
            get => NetActorNode.Exp;
            set => NetActorNode.Exp = value;
        }
        public String Cid => m_cId;
        public CharacterEventSystem CharacterEventSystem => m_characterEventSystem;
        public GameTaskConditionChecker GameTaskConditionChecker => m_gameTaskConditionChecker; 
        #endregion

        #region 生命周期
        public void Init(string sessionId,Connection conn, CharacterEnterSceneRequest message)
        {
            var dbChrNode = message.DbChrNode;
            m_cId = dbChrNode.CId;
            m_session = new Session(sessionId, conn);
            m_session.Chr = this;

            var initPos = new NetVector3 { X = dbChrNode.ChrStatus.X, Y = dbChrNode.ChrStatus.Y, Z = dbChrNode.ChrStatus.Z };
            base.Init(initPos, dbChrNode.ProfessionId, dbChrNode.Level, message.Equips);

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

            // 事件系统
            m_characterEventSystem = new CharacterEventSystem();
            m_gameTaskConditionChecker = new GameTaskConditionChecker();
            m_gameTaskConditionChecker.Init(this, message.NeedListenConds);
        }
        #endregion

        #region 角色行为
        private static readonly string ReachPositionEvent = GameEventType.ReachPosition.ToString();
        private readonly Dictionary<string, object> _cachedPositionParams = new()
        {
            { "Position", default(Vector3) } // 初始值可随意设置
        };
        public override void SetTransform(NetTransform transform)
        {
            base.SetTransform(transform);

            // 角色位置变化事件
            Vector3 vec = new Vector3
            {
                x = Position.x * 0.001f,
                y = Position.y * 0.001f,
                z = Position.z * 0.001f,
            };
            _cachedPositionParams["Position"] = vec;
            m_characterEventSystem.Trigger(ReachPositionEvent, _cachedPositionParams);
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
            if (deltaLevel > 0)
            {
                UpgradeLevel(deltaLevel);
            }

            //发包
            ActorPropertyUpdate po = new()
            {
                EntityId = EntityId,
                PropertyType = PropertyType.Exp,
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
                PropertyType = PropertyType.Level,
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
            ForceChangeSelfActor(NetActorState.Death);
        }
        protected override void ForceChangeSelfActor(NetActorState state)
        {
            base.ForceChangeSelfActor(state);
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
        #endregion

        #region Tools
        public void Send(IMessage message)
        {
            m_session.Send(message);
        }
        public void Send(Scene2GateMsg msg)
        {
            msg.SessionId = m_session.SesssionId;
            m_session.Send(msg);
        }
        #endregion

        #region AOI
        public override void OnUnitEnter(IAOIUnit unit)
        {
            if(unit == null) return;
            OtherEntityEnterSceneResponse resp = new();
            resp.SceneId = SceneManager.Instance.SceneId;
            resp.SessionId = SessionId;
            if (unit is SceneActor actor)
            {
                resp.EntityType = SceneEntityType.Actor;
                resp.ActorNode = actor.NetActorNode;
            }
            else if (unit is SceneItem item)
            {
                resp.EntityType = SceneEntityType.Item;
                resp.ItemNode = item.NetItemNode;
            }
            Send(resp);
        }
        public override void OnUnitLeave(IAOIUnit unit)
        {
            if (unit == null) return;
            SceneEntity entity = unit as SceneEntity;
            var resp = new OtherEntityLeaveSceneResponse();
            resp.SceneId = SceneManager.Instance.SceneId;
            resp.SessionId = SessionId;
            resp.EntityId = entity.EntityId;
            Send(resp);
        }
        public override void OnPosError()
        {
            base.OnPosError();
/*            //传送回默认出生点
            Space sp = null;
            if (currentSpace == null)
            {
                sp = SpaceManager.Instance.GetSpaceById(0);
            }
            else
            {
                sp = currentSpace;
            }
            var pointDef = DataManager.Instance.revivalPointDefindeDict.Values.Where(def => def.SID == sp.SpaceId).First();
            if (pointDef != null)
            {
                TransmitSpace(sp, new Core.Vector3Int(pointDef.X, pointDef.Y, pointDef.Z));
            }*/
        }

        #endregion
    }
}
