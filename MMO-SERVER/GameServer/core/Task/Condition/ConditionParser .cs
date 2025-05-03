using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition
{
    public class ConditionParser
    {
        private Dictionary<string, IConditionChecker> m_checkers = new Dictionary<string, IConditionChecker>();

        public void RegisterChecker(string prefix, IConditionChecker checker)
        {
            // prefix : kill  level
            m_checkers[prefix] = checker;
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
