using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition.Impl
{
    // 示例：检查玩家等级
    public class LevelConditionChecker : IConditionChecker
    {
        public bool InitCondition(ConditionData condition, GameCharacter chr)
        {
            int targetLevel = int.Parse(condition.Parameters[0]);
            if(chr.Level >= targetLevel)
            {
                condition.CurValue = 1;
            }
            return true;
        }

        public bool UnInitCondition(ConditionData condition, GameCharacter chr)
        {
            return true;
        }

        public bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args)
        {
            // Level:1
            int targetLevel = int.Parse(condition.Parameters[0]);
            if (chr.Level >= targetLevel)
            {
                condition.CurValue = 1;
            }
            return true;
        }
    }
}
