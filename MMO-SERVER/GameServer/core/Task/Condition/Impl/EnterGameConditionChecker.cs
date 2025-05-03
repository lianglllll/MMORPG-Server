using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition.Impl
{
    public class EnterGameConditionChecker : IConditionChecker
    {
        public bool InitCondition(ConditionData condition, GameCharacter chr)
        {
            return true;
        }

        public bool IsConditionMet(ConditionData condition, object arg)
        {
            throw new NotImplementedException();
        }

        public bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args)
        {
            condition.CurValue++;
            return true;
        }
    }
}
