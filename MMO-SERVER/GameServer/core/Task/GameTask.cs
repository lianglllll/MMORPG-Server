using Common.Summer.Tools;
using GameServer.Core.Model;
using GameServer.Core.Task.Condition;
using GameServer.Core.Task.Reward;
using GameServer.Utils;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBTask;
using HS.Protobuf.GameTask;
using HS.Protobuf.Scene;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GameServer.Core.Task
{
    [Serializable]
    public class ConditionData
    {
        public string condType;         // 条件类型：KillEnemy/ReachPosition等
        public string[] Parameters;     // 参数：[1001] 或 [200,1,100]
        public int TargetValue;         // 目标值
        public int CurValue;
    }

    [Serializable]
    public class RewardData
    {
        public int taskId;
        public string rewardType;         
        public string[] Parameters;     
    }

    // 任务实例
    public class GameTask : IStateMachineOwner
    {
        private GameCharacter m_owner;
        private TaskDefine m_taskDefine;

        // 动态信息
        private List<ConditionData> m_progress;    
        private GameTaskState       m_curState;               
        private StateMachine        m_machine;
        private List<RewardData>    m_reward;

        #region GetSet
        public TaskDefine TaskDefine => m_taskDefine;
        public GameTaskState GameTaskState
        {
            get => m_curState;
            set => m_curState = value;
        }
        public DBTaskNode DBTaskNode
        {
            get
            {
                var tNode = new DBTaskNode();
                tNode.TaskId = m_taskDefine.Task_id;
                tNode.State = (int)m_curState;
                if(m_progress != null)
                {
                    tNode.TaskProgress = JsonConvert.SerializeObject(m_progress);
                }
                return tNode;
            }
        }
        public NetGameTaskNode NetGameTaskNode
        {
            get
            {
                var tNode = new NetGameTaskNode();
                tNode.TaskId = m_taskDefine.Task_id;
                tNode.TaskState = m_curState;
                if(m_progress != null)
                {
                    tNode.TaskProgress = JsonConvert.SerializeObject(m_progress);
                }
                return tNode;
            }
        }
        public List<ConditionData> Progress => m_progress;
        public List<RewardData> Reward => m_reward;
        public int TaskId => m_taskDefine.Task_id;
        public GameCharacter Owner => m_owner;
        #endregion

        #region 生命周期
        public void Init(GameCharacter chr, TaskDefine def, DBTaskNode dbNode = null)
        {
            m_taskDefine = def;
            m_owner = chr;
            m_machine = new();
            m_machine.Init(this);

            if (dbNode == null)
            {
                // 第一次的
                ChangeState(GameTaskState.Locked, true, true);
            }
            else
            {
                // 动态信息设置
                var tmpState = (GameTaskState)dbNode.State;
                try
                {
                    if(dbNode.TaskProgress != null)
                    {
                        m_progress = JsonConvert.DeserializeObject<List<ConditionData>>(dbNode.TaskProgress);
                    }
                    ChangeState(tmpState,false, true);
                }
                catch (Exception ex)
                {
                    // 日志错误，重置任务状态
                    Log.Error($"任务 {dbNode.TaskId} 进度解析失败: {ex.Message}");
                    ChangeState(tmpState,true, true);
                }
            }
        }
        public void UpdateProgress(string conditionKey, Dictionary<string, object> args)
        {
            if(m_curState != GameTaskState.Locked && m_curState != GameTaskState.InProgress)
            {
                return;
            }
            GameTaskStateBase state = m_machine.CurState as GameTaskStateBase;
            state.UpdateProgress(conditionKey, m_owner, args);

            SendGameTaskProgressChangeMsg();
        }
        public void GrantRewards()
        {
            if(m_reward == null)
            {
                goto End;
            }
            if(m_curState != GameTaskState.Completed)
            {
                goto End;
            }
            foreach(var item in m_reward)
            {
                TaskRewardParser.Instance.GrantRewards(item, Owner);
            }
            ChangeState(GameTaskState.Rewarded, false, true);
        End:
            return;
        }
        #endregion

        #region tools
        public void ChangeState(GameTaskState newState, bool isReEntry = false, bool dontSendMsg = false)
        {
            if (m_curState == newState && !isReEntry) return;
            m_curState = newState;
            switch(newState)
            {
                case GameTaskState.Locked:
                    if(m_progress == null)
                    {
                        m_progress = ParseConditionsStr(m_taskDefine.Pre_conditions);
                        foreach (var condition in m_progress)
                        {
                            TaskConditionParser.Instance.InitCondition(condition, Owner);
                        }
                    }
                    m_machine.ChangeState<GameTaskLockState>();
                    break;
                case GameTaskState.Unlocked:
                    m_machine.ChangeState<GameTaskUnLockState>();
                    break;
                case GameTaskState.InProgress:
                    // 任务第一次进入这个状态
                    if(m_progress == null)
                    {
                        m_progress = ParseConditionsStr(m_taskDefine.Target_conditions);
                    }
                    foreach (var condition in m_progress)
                    {
                        TaskConditionParser.Instance.InitCondition(condition, Owner);
                    }
                    m_machine.ChangeState<GameTaskInProgressState>();
                    break;
                case GameTaskState.Completed:
                    m_reward = ParseRewardsStr(m_taskDefine.Reward_items);
                    m_machine.ChangeState<GameTaskCompletedState>();
                    break;
                case GameTaskState.Rewarded:
                    m_machine.ChangeState<GameTaskRewardedState>();
                    break;
            }
            if (!dontSendMsg)
            {
                SendGameTaskStateChangeMsg();
            }
        }
        public void ResetTask()
        {
            ChangeState(GameTaskState.InProgress, true);
        }
        public void ClearProgress()
        {
            m_progress = null;
        }
        public void ClearReward()
        {
            m_reward = null;
        }
        public List<ConditionData> ParseConditionsStr(string conditions)
        {
            var dict = new List<ConditionData>();
            if (string.IsNullOrEmpty(conditions)) return dict;

            // 示例条件格式："KillEnemy:1001=5;ReachPosition:200,1,100;Level:1"
            foreach (var clause in conditions.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = clause.Split('=', 2);
                int targetValue = 1;    // 如果不写= 就默认值为1
                if (parts.Length == 2)
                {
                    targetValue = int.Parse(parts[1]);
                }
                var typeParts = parts[0].Split(new[] { ':' }, 2);
                var conditionType = typeParts[0];
                dict.Add(new ConditionData
                {
                    condType = conditionType,
                    Parameters = typeParts.Length > 1 ? typeParts[1].Split(',') : Array.Empty<string>(),
                    TargetValue = targetValue,
                    CurValue = 0
                });
            }
            return dict;
        }
        private List<RewardData> ParseRewardsStr(string rewards)
        {
            var dict = new List<RewardData>();
            if (string.IsNullOrEmpty(rewards)) return dict;

            // 示例条件格式："Item:1001,5;Item:1002,1;Skill:1222"
            foreach (var clause in rewards.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var typeParts = clause.Split(new[] { ':' }, 2);
                var rewardType = typeParts[0];
                dict.Add(new RewardData
                {
                    taskId = TaskId,
                    rewardType = rewardType,
                    Parameters = typeParts.Length > 1 ? typeParts[1].Split(',') : Array.Empty<string>(),
                });
            }
            return dict;
        }

        private void SendGameTaskStateChangeMsg()
        {
            GameTaskChangeOperationResponse resp = new();
            resp.TaskId = m_taskDefine.Task_id;
            resp.Opration = GameTaskChangeOperationType.State;

            // args
            resp.NewState = m_curState;
            if(m_curState == GameTaskState.Locked || m_curState == GameTaskState.InProgress) { 
                resp.NewConditions = JsonConvert.SerializeObject(m_progress);
            }

            resp.SessionId = Owner.SessionId;
            Owner.SendToGate(resp);
        }
        private void SendGameTaskProgressChangeMsg()
        {
            GameTaskChangeOperationResponse resp = new();
            resp.TaskId = m_taskDefine.Task_id;
            resp.Opration = GameTaskChangeOperationType.Condition;

            // args
            resp.NewConditions = JsonConvert.SerializeObject(m_progress);

            resp.SessionId = Owner.SessionId;
            Owner.SendToGate(resp);
        }

        public bool CheckAndRegisterConditionToSceneServer()
        {
            bool isNeedSend = false;

            var req = new RegisterTaskConditionToSceneRequest();
            req.EntityId = m_owner.EntityId;
            req.TaskId = TaskId;
            foreach (var condition in Progress)
            {
                if(!TaskConditionParser.Instance.IsNeedRegisterToScene(condition, Owner))
                {
                    continue;
                }
                isNeedSend = true;

                var dataNode = new TaskConditionDataNode();
                dataNode.TaskId = TaskId;
                dataNode.ConditionType = condition.condType;
                dataNode.Parameters.Add(condition.Parameters);
                req.Conds.Add(dataNode);
            }
            if (isNeedSend && Owner.GameTaskManager.IsInit)
            {
                // 初始化的时候也可能调用发送，但是其实不需要的。
                m_owner.SendToScene(req);
            }
            return isNeedSend;
        }
        public bool UnRegisterConditionToSceneServer()
        {
            if (!Owner.GameTaskManager.IsInit) return true;

            var req = new UnRegisterTaskConditionToSceneRequest();
            req.EntityId = m_owner.EntityId;    
            req.TaskId = TaskId;
            foreach(var condition in Progress)
            {
                if(TaskConditionParser.Instance.IsNeedRegisterToScene(condition, Owner))
                {
                    req.CondTypes.Add(condition.condType);
                }
            }
            m_owner.SendToScene(req);
            return true;
        }
        #endregion
    }

    public class GameTaskStateBase : StateBase
    {
        protected GameTask m_gameTask;
        protected GameTaskManager m_taskManager;
        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            m_gameTask = (GameTask)owner;
            m_taskManager = m_gameTask.Owner.GameTaskManager;
        }
        public virtual void UpdateProgress(string conditionType, GameCharacter m_owner, Dictionary<string, object> args)
        {

        }
    }
    public class GameTaskLockState : GameTaskStateBase
    {
        private bool isRegisterToScene;

        public override void Enter()
        {
            isRegisterToScene = false;

            // 注册未满足的解锁条件
            foreach (var cond in m_gameTask.Progress)
            {
                m_taskManager.Subscribe(cond.condType, m_gameTask);
            }

            if (CheckUnlockConditions())
            {
                m_gameTask.ChangeState(GameTaskState.Unlocked);
            }
            else
            {
                // 看看有没有条件是需要scene支持的
                isRegisterToScene = m_gameTask.CheckAndRegisterConditionToSceneServer();
            }
        }
        public override void Exit()
        {
            // 反注册
            foreach (var cond in m_gameTask.Progress)
            {
                m_taskManager.UnSubscribe(cond.condType, m_gameTask);
            }

            if (isRegisterToScene)
            {
                m_gameTask.UnRegisterConditionToSceneServer();
            }

            m_gameTask.ClearProgress();
        }
        public override void UpdateProgress(string conditionType, GameCharacter m_owner, Dictionary<string, object> args)
        {
            foreach(var cond in m_gameTask.Progress)
            {
                if (cond.condType != conditionType) continue;
                TaskConditionParser.Instance.UpdateCondition(cond, m_owner, args);
            }

            if (CheckUnlockConditions())
            {
                m_gameTask.ChangeState(GameTaskState.Unlocked);
            }
        }
        private bool CheckUnlockConditions()
        {
            bool result = false;
            foreach (var cond in m_gameTask.Progress)
            {
                if(cond.CurValue < cond.TargetValue)
                {
                    goto End;
                }
            }
            result = true;
        End:
            return result;
        }
    }
    public class GameTaskUnLockState : GameTaskStateBase
    {
        public override void Enter()
        {
        }
    }
    public class GameTaskInProgressState : GameTaskStateBase
    {
        private bool isRegisterToScene;

        public override void Enter()
        {
            isRegisterToScene = false;

            // 注册未满足的解锁条件
            foreach (var cond in m_gameTask.Progress)
            {
                m_taskManager.Subscribe(cond.condType, m_gameTask);
            }

            // 检查是否完成
            if (CheckCompelteConditions())
            {
                m_gameTask.ChangeState(GameTaskState.Completed);
            }
            else
            {
                // 看看有没有条件是需要scene支持的
                isRegisterToScene = m_gameTask.CheckAndRegisterConditionToSceneServer();
            }
        }
        public override void Exit()
        {
            // 反注册
            foreach (var cond in m_gameTask.Progress)
            {
                m_taskManager.UnSubscribe(cond.condType, m_gameTask);
            }

            if (isRegisterToScene)
            {
                m_gameTask.UnRegisterConditionToSceneServer();
            }

            m_gameTask.ClearProgress();
        }
        public override void UpdateProgress(string conditionType, GameCharacter m_owner, Dictionary<string, object> args)
        {
            foreach (var cond in m_gameTask.Progress)
            {
                if (cond.condType != conditionType) continue;
                TaskConditionParser.Instance.UpdateCondition(cond, m_owner, args);
            }
            if (CheckCompelteConditions())
            {
                m_gameTask.ChangeState(GameTaskState.Completed);
            }
        }
        private bool CheckCompelteConditions()
        {
            bool result = false;
            foreach (var cond in m_gameTask.Progress)
            {
                if (cond.CurValue < cond.TargetValue)
                {
                    goto End;
                }
            }
            result = true;
        End:
            return result;
        }
    }
    public class GameTaskCompletedState : GameTaskStateBase
    {
        public override void Enter()
        {
            // 如果没奖励的话直接跳过
            if(m_gameTask.Reward.Count == 0)
            {
                m_gameTask.ChangeState(GameTaskState.Rewarded);
            }
        }
        public override void Exit()
        {
            m_gameTask.ClearReward();
        }
    }
    public class GameTaskRewardedState : GameTaskStateBase
    {
        public override void Enter()
        {
            // 根据不同的任务开启不同的事情
            switch (m_gameTask.TaskDefine.Task_type)
            {
                case (int)GameTaskType.MainStory:
                    OnMainStroy();
                    break;
                case (int)GameTaskType.Common:
                    OnCommon();
                    break;
                default:
                    break;
            }
            TaskEnd();
        }
        private void OnMainStroy()
        {
            // 1.接着当前的主线子编号
            var def = m_gameTask.TaskDefine;
            int chainId = def.Chain_id;
            int nextSubId = def.Sub_id + 1;
            var nextTaskDef = StaticDataManager.Instance.GetChainTaskDefine(chainId, nextSubId);
            m_taskManager.AddTask(nextTaskDef);

            // 2.可能会开启支线
            var taskChainIds = def.Next_chains;
            if(taskChainIds == null || taskChainIds.Length == 0)
            {
                goto End;
            }
            foreach(var taskChainId in taskChainIds)
            {
                def = StaticDataManager.Instance.GetChainTaskDefine(taskChainId, 1);
                if(def == null)
                {
                    continue;
                }
                m_taskManager.AddTask(def);
            }



        End:
            return;
        }
        private void OnSideStory()
        {
            // 接着当前的支线线子编号
            var def = m_gameTask.TaskDefine;
            int chainId = def.Chain_id;
            int nextSubId = def.Sub_id + 1;
            var nextTaskDef = StaticDataManager.Instance.GetChainTaskDefine(chainId, nextSubId);
            m_taskManager.AddTask(nextTaskDef);
        }
        private void OnCommon()
        {
            // 不做其他事情
        }
        private void TaskEnd()
        {
            if(m_gameTask.TaskDefine.Task_type == (int)GameTaskType.Daily)
            {
                goto End;
            }
            // 通知taskMgr删除当前的Task
            m_taskManager.RemoveTask(m_gameTask.TaskId);

        End:
            return;
        }
    }
}
