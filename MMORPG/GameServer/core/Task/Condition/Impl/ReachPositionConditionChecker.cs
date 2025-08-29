using Common.Summer.Core;
using GameServer.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task.Condition.Impl
{
    public class ReachPositionConditionChecker : IConditionChecker
    {
        public bool InitCondition(ConditionData condition, GameCharacter chr)
        {
            return true;
        }
        public bool UnInitCondition(ConditionData condition, GameCharacter chr)
        {
            return true;
        }
        public bool UpdateCondition(ConditionData condition, GameCharacter chr, Dictionary<string, object> args)
        {
            if(condition.CurValue == 1)
            {
                goto End;
            }
            Vector3 curPosition = (Vector3)args["Position"];
            Vector3 targetPosition = new Vector3()
            {
                x = int.Parse(condition.Parameters[0]),
                y = int.Parse(condition.Parameters[1]),
                z = int.Parse(condition.Parameters[2]),
            };
            var dist = Vector3.Distance(curPosition, targetPosition);
            if(dist < 5)
            {
                condition.CurValue = condition.TargetValue;
            }
        End:
            return true;
        }
        public bool IsNeedRegisterToScene() { return true; }
        public Dictionary<string, object> ParseRemoteArgs(string args)
        {
            // "100,1,100"
            var parts = args.Split(',');
            Vector3 position = new Vector3()
            {
                x = float.Parse(parts[0]),
                y = float.Parse(parts[1]),
                z = float.Parse(parts[2]),
            };
            return new Dictionary<string, object> { { "Position", position } };
        }
    }
}
