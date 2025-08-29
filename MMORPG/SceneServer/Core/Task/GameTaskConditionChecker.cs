using Common.Summer.Core;
using Common.Summer.Tools.GameEvent;
using Google.Protobuf;
using Google.Protobuf.Collections;
using HS.Protobuf.GameTask;
using HS.Protobuf.Scene;
using SceneServer.Core.Model.Actor;
using SceneServer.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Task
{
    public class GameTaskConditionChecker
    {
        private SceneCharacter m_owner;
        private Dictionary<string, List<TaskConditionDataNode>> conditions = new();

        
        public bool Init(SceneCharacter chr, Google.Protobuf.Collections.RepeatedField<RegisterTaskConditionToSceneRequest> needListenConds)
        {
            m_owner = chr;
            m_owner.CharacterEventSystem.Subscribe(ReachPositionEvent, HandleReachPositionEvent);

            if(needListenConds != null)
            {
                foreach(var req in needListenConds)
                {
                    RegirsterTaskCondition(req.Conds);
                }
            }
            return true;
        }

        public bool RegirsterTaskCondition(RepeatedField<TaskConditionDataNode> conds)
        {
            foreach(var cond in conds)
            {
                string condType = cond.ConditionType;
                conditions.TryGetValue(condType, out var list);
                if (list == null)
                {
                    list = new List<TaskConditionDataNode>();
                    conditions[condType] = list;
                }
                list.Add(cond);
            }
            return true;
        }
        public void UnRegirsterTaskCondition(int taskId, RepeatedField<string> condTypes)
        {
            foreach(var cond in condTypes)
            {
                if(conditions.TryGetValue((string)cond, out var list))
                {
                    list.RemoveAll(c => c.TaskId == taskId);
                    if (list.Count == 0)
                    {
                        conditions.Remove(cond);
                    }
                }

            }
        }

        // 处理事件
        private static readonly string ReachPositionEvent = GameEventType.ReachPosition.ToString();
        private void HandleReachPositionEvent(Dictionary<string, object> dictionary)
        {
            conditions.TryGetValue(ReachPositionEvent, out var list);
            if(list == null || list.Count == 0)
            {
                goto End;
            }

            // 检查是否匹配，如果是就通知game
            Vector3 position = (Vector3)dictionary["Position"];

            foreach(var cond in list)
            {
                Vector3 targetPosition = new Vector3(int.Parse(cond.Parameters[0]), int.Parse(cond.Parameters[1]), int.Parse(cond.Parameters[2]));
                float distance = Vector3.Distance(position, targetPosition);
                if(distance > 5)
                {
                    continue;
                }
                
                // 通知Game
                var resp = new SecneTriggerTaskConditionResponse();
                resp.CId = m_owner.Cid;
                resp.CondType = cond.ConditionType;
                resp.Parameter = $"{targetPosition.x},{targetPosition.y},{targetPosition.z}";
                ServersMgr.Instance.SendToGame(resp);
            }
        End:
            return;
        }


    }
}
