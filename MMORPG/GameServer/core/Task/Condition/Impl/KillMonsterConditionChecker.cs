using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition.Impl
{
    public class KillMonsterConditionChecker : IConditionChecker
    {
        public bool InitCondition(ConditionData condition, GameCharacter chr)
        {
            // 主要是设置target 和 cur
            condition.CurValue = 0;
            condition.TargetValue = int.Parse(condition.Parameters[1]);
            return true;
        }

        public bool UnInitCondition(ConditionData condition, GameCharacter chr)
        {
            return true;
        }

        public bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args)
        {
            // KillMonster:1001=2
            int targetPid = int.Parse(condition.Parameters[0]);
            int curPid = (int)args["professionId"];
            if(targetPid != curPid)
            {
                return false;
            }
            condition.CurValue += 1;
            return true;
        }

        bool IConditionChecker.InitCondition(ConditionData condition, GameCharacter chr)
        {
            throw new NotImplementedException();
        }
    }
}
