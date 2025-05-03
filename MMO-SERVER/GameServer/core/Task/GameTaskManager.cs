using Common.Summer.Core;
using GameServer.Core.Model;
using GameServer.Core.Task.Condition;
using GameServer.Core.Task.Condition.Impl;
using GameServer.Core.Task.Event;
using GameServer.Utils;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.DBProxy.DBTask;
using HS.Protobuf.GameTask;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Task
{
    public class GameTaskManager
    {
        
        private GameCharacter m_owner;
        private Dictionary<int, GameTask> m_allTasks = new Dictionary<int, GameTask>();

        private Dictionary<string, Dictionary<int, GameTask>> conditions = new();
        private ConditionParser m_conditionParser = new ConditionParser();                  // 用于自定义判断任务条件的是否完成

        public ConditionParser ConditionParser => m_conditionParser;

        public void Init(GameCharacter owner, DBCharacterTasks tasks)
        {
            m_owner = owner;

            // 注册条件检查器
            m_conditionParser.RegisterChecker("Level", new LevelConditionChecker());
            m_conditionParser.RegisterChecker("Task", new TaskCompletedChecker());
            m_conditionParser.RegisterChecker("EnterGame", new EnterGameConditionChecker());
            m_conditionParser.RegisterChecker("ReachPosition", new ReachPositionConditionChecker());
            m_conditionParser.RegisterChecker("TalkNpc", new TalkNpcConditionChecker());
            m_conditionParser.RegisterChecker("CastSkill", new CastSkillConditionChecker());
            m_conditionParser.RegisterChecker("KillMonster", new KillMonsterConditionChecker());
            m_conditionParser.RegisterChecker("CollectItem", new CollectItemConditionChecker());

            // 事件注册
            m_owner.CharacterEventSystem.Subscribe("KillMonster", HandleKillMonsterEvent);
            m_owner.CharacterEventSystem.Subscribe("CollectItem", HandleCollectItemEvent);
            m_owner.CharacterEventSystem.Subscribe("LevelUp", HandleLevelUpEvent);
            m_owner.CharacterEventSystem.Subscribe("EnterGame", HandleEnterGameEvent);

            // 解析我们现有的数据
            LoadTasks(tasks);

            // 示例：每天 04:00 刷新
            // 处理日常任务和限时任务
            DateTime nextRefresh = DateTime.Today.AddDays(1).AddHours(4);
            TimeSpan delay = nextRefresh - DateTime.Now;
            Scheduler.Instance.AddTask(RefreshDailyTasks, delay.Milliseconds, 1);
        }
        private void RefreshDailyTasks()
        {
            foreach (var task in m_allTasks.Values)
            {
                if (task.TaskDefine.Task_type == (int)GameTaskType.Daily)
                {
                    // 重置任务状态
                    task.ResetTask();
                }
            }
            // 重新计时
            // todo
        }
        public List<GameTask> GetTasksByType(GameTaskType type)
        {
            List<GameTask> result = new List<GameTask>();
            foreach (var task in m_allTasks.Values)
            {
                if (task.TaskDefine.Task_type == (int)type)
                {
                    result.Add(task);
                }
            }
            return result;
        }
        public bool AddTask(TaskDefine def)
        {
            bool result = false;
            if(def == null)
            {
                Log.Warning($"任务Def为空");
                goto End;
            }
            
            // 存在性检查
            int taskId = def.Task_id;
            if (m_allTasks.ContainsKey(taskId))
            {
                Log.Warning($"任务{taskId}已存在");
                goto End;
            }

            // 构建任务实例
            var gameTask = new GameTask();
            gameTask.Init(m_owner, def, null);
            m_allTasks.Add(taskId, gameTask);

        End:
            return result;
        }
        public bool RemoveTask(int taskId)
        {
            bool result = false;
            if(!m_allTasks.ContainsKey(taskId))
            {
                goto End;
            }
            m_allTasks.Remove(taskId);
        End:
            return result;
        }

        // 持久化
        public DBCharacterTasks GetDBTaskNodes()
        {
            // 将当前任务全部打包成dbnode发到数据库代理中
            DBCharacterTasks dbCharacterTasks = new DBCharacterTasks();
            foreach (var taskEntry in m_allTasks)
            {
                var gameTask = taskEntry.Value;
                dbCharacterTasks.Tasks.Add(gameTask.DBTaskNode);
            }
            return dbCharacterTasks;
        }
        private void LoadTasks(DBCharacterTasks tasks)
        {
            foreach (var dbNode in tasks.Tasks)
            {
                var define = StaticDataManager.Instance.GetTaskDefineByTaskId(dbNode.TaskId);
                if (define == null)
                {
                    continue; // 忽略无效任务
                }
                
                var gameTask = new GameTask();
                gameTask.Init(m_owner, define, dbNode);
                m_allTasks[dbNode.TaskId] = gameTask;
            }
        }

        #region 事件处理
        private void HandleKillMonsterEvent(Dictionary<string, object> args)
        {
            string conditionTypte = "KillMonster";
            conditions.TryGetValue(conditionTypte, out var tasks);
            if (tasks == null) return;
            foreach(var task in tasks.Values)
            {
                task.UpdateProgress(conditionTypte, args);
            }
        }
        private void HandleCollectItemEvent(Dictionary<string, object> args)
        {
        }
        private void HandleLevelUpEvent(Dictionary<string, object> args)
        {
            string conditionTypte = "Level";
            conditions.TryGetValue(conditionTypte, out var tasks);
            if (tasks == null) return;
            foreach (var task in tasks.Values)
            {
                task.UpdateProgress(conditionTypte, args);
            }
        }
        private void HandleEnterGameEvent(Dictionary<string, object> args)
        {
            string conditionTypte = "EnterGame";
            conditions.TryGetValue(conditionTypte, out var tasks);
            if (tasks == null) return;
            foreach (var task in tasks.Values)
            {
                task.UpdateProgress(conditionTypte, args);
            }
        }
        #endregion

        // tools
        public bool Subscribe(string conditionType, GameTask gameTask)
        {
            bool result = false;
            if (!conditions.ContainsKey(conditionType))
            {
                conditions.Add(conditionType, new Dictionary<int, GameTask>());
            }
            var tasks = conditions[conditionType];
            if (tasks.ContainsKey(gameTask.TaskId))
            {
                goto End;
            }
            tasks.Add(gameTask.TaskId, gameTask);
            result = true;
        End:
            return result;
        }
        public bool UnSubscribe(string conditionType, GameTask gameTask) { 
            bool result = false;    
            if(!conditions.ContainsKey(conditionType))
            {
                goto End;
            }
            var tasks = conditions[conditionType];
            if (!tasks.ContainsKey(gameTask.TaskId))
            {
                goto End;
            }
            tasks.Remove(gameTask.TaskId);
            if (tasks.Count == 0)
            {
                conditions.Remove(conditionType);
            }
            result = true;
        End:
            return result;
        }
        public List<NetGameTaskNode> GetAllActiveTask()
        {
            var list = new List<NetGameTaskNode>();
            foreach(var task in m_allTasks.Values)
            {
                list.Add(task.NetGameTaskNode);
            }
            return list;
        }
    }
}
