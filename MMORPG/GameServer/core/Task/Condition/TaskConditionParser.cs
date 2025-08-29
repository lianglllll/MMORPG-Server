using Common.Summer.Net;
using Common.Summer.Tools;
using GameServer.Core.Model;
using GameServer.Core.Task.Condition.Impl;
using GameServer.Net;
using HS.Protobuf.Chat;
using HS.Protobuf.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition
{
    public class TaskConditionParser : Singleton<TaskConditionParser>
    {
        private Dictionary<string, IConditionChecker> m_checkers = new Dictionary<string, IConditionChecker>();

        public  override void Init()
        {
            m_checkers["Level"] = new LevelConditionChecker();
            m_checkers["Task"] = new TaskCompletedChecker();
            m_checkers["EnterGame"] = new EnterGameConditionChecker();
            m_checkers["ReachPosition"] = new ReachPositionConditionChecker();
            m_checkers["TalkNpc"] = new TalkNpcConditionChecker();
            m_checkers["CastSkill"] = new CastSkillConditionChecker();
            m_checkers["KillMonster"] = new KillMonsterConditionChecker();
            m_checkers["CollectItem"] = new CollectItemConditionChecker();

            // 协议注册
            ProtoHelper.Instance.Register<RegisterTaskConditionToSceneRequest>((int)SceneProtocl.RegisterTaskConditionToSceneReq);
            ProtoHelper.Instance.Register<RegisterTaskConditionToSceneresponse>((int)SceneProtocl.RegisterTaskConditionToSceneResp);
            ProtoHelper.Instance.Register<UnRegisterTaskConditionToSceneRequest>((int)SceneProtocl.UnRegisterTaskConditionToSceneReq);
            ProtoHelper.Instance.Register<UnRegisterTaskConditionToSceneResponse>((int)SceneProtocl.UnRegisterTaskConditionToSceneResp);

        }
        public bool InitCondition(ConditionData data, GameCharacter chr)
        {
            var checker = m_checkers[data.condType];
            checker.InitCondition(data, chr);
            return true;
        }
        public bool UnInitCondition(ConditionData data, GameCharacter chr)
        {
            var checker = m_checkers[data.condType];
            checker.UnInitCondition(data, chr);
            return true;
        }
        public bool UpdateCondition(ConditionData data, GameCharacter chr, Dictionary<string, object> args)
        {
            var checker = m_checkers[data.condType];
            checker.UpdateCondition(data, chr, args);
            return true;
        }
        public bool IsNeedRegisterToScene(ConditionData data, GameCharacter chr)
        {
            var checker = m_checkers[data.condType];
            return checker.IsNeedRegisterToScene();
        }
        public Dictionary<string, object> ParseRemoteArgs(string condType, string args)
        {
            var checker = m_checkers[condType];
            return checker.ParseRemoteArgs(args);
        }
    }
}
