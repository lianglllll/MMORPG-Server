using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition
{
    public interface IConditionChecker
    {
        bool IsConditionMet(ConditionData condition, object arg); // context可以是玩家数据、任务状态ConditionData等
        public bool InitCondition(ConditionData condition, GameCharacter chr);
        bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args);
    }
}
