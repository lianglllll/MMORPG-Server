using GameServer.Core.Model;
using GameServer.Core.Task.Condition.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition.Impl
{
    public class TaskCompletedChecker : IConditionChecker
    {
        public bool InitCondition(ConditionData condition, GameCharacter chr)
        {
            return true;
        }

        public bool IsConditionMet(ConditionData condition, object arg)
        {
            var player = (GameCharacter)arg;
            int questId = int.Parse(condition.Parameters[0]);

            // return player.CompletedQuests.Contains(questId);
            return false;
        }

        public bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args)
        {
            throw new NotImplementedException();
        }
    }
}
