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

        public bool IsConditionMet(ConditionData condition, object arg)
        {
            // 假设 arg 是玩家对象
            GameCharacter player = (GameCharacter)arg;
            if(player.Level >= condition.TargetValue)
            {
                return true;
            }
            return false;
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
