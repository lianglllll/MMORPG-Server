using Common.Summer.Tools;
using GameServer.Core.Model;
using GameServer.Core.Task.Condition.Impl;
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

        public void Init()
        {
            m_checkers["Level"] = new LevelConditionChecker();
            m_checkers["Task"] = new TaskCompletedChecker();
            m_checkers["EnterGame"] = new EnterGameConditionChecker();
            m_checkers["ReachPosition"] = new ReachPositionConditionChecker();
            m_checkers["TalkNpc"] = new TalkNpcConditionChecker();
            m_checkers["CastSkill"] = new CastSkillConditionChecker();
            m_checkers["KillMonster"] = new KillMonsterConditionChecker();
            m_checkers["CollectItem"] = new CollectItemConditionChecker();

        }
        public bool InitCondition(ConditionData data, GameCharacter chr)
        {
            var checker = m_checkers[data.condType];
            checker.InitCondition(data, chr);
            return true;
        }
        public bool UpdateCondition(ConditionData data, GameCharacter chr, Dictionary<string, object> args)
        {
            var checker = m_checkers[data.condType];
            checker.UpdateCondition(data, chr, args);
            return true;
        }
    }
}
