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
        public bool InitCondition(ConditionData condition, GameCharacter chr);
        public bool UnInitCondition(ConditionData condition, GameCharacter chr);
        public bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args);

        public bool IsNeedRegisterToScene() {  return false; }
        public Dictionary<string, object> ParseRemoteArgs(string args)
        {
            return null;
        }
    }
}
