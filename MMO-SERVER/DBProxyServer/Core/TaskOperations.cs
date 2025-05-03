using MongoDB.Bson;
using MongoDB.Driver;
using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.Linq;
using HS.Protobuf.DBProxy.DBTask;
using MongoDB.Bson.Serialization;
using Serilog;
using System.Reflection.Metadata;

namespace DBProxyServer.Core
{
    public class TaskOperations : Singleton<TaskOperations>
    {
        private IMongoCollection<BsonDocument>? m_taskCollection;

        public void Init(MongoDBConnection dbConnection)
        {
            m_taskCollection = dbConnection.GetCollection<BsonDocument>("Task");
        }

        public async Task<List<DBTaskNode>> GetDBTaskNodesByCid(string cid)
        {
            List<DBTaskNode> result = new();

            var filter = Builders<BsonDocument>.Filter.Eq("cid", cid);
            var document = await m_taskCollection.Find(filter).FirstOrDefaultAsync();
            if (document == null || !document.Contains("tasks"))
                return new List<DBTaskNode>();

            // 提取 tasks 数组并转换
            var tasksArray = document["tasks"].AsBsonArray;
            return tasksArray
                .Where(t => t.IsBsonDocument)
                .Select(t => new DBTaskNode
                {
                    TaskId = t["taskId"].AsInt32,
                    State = t["state"].AsInt32,
                    TaskProgress = t["taskProgress"].IsBsonNull ? "" : t["taskProgress"].AsString,
                    StartTime = t["startTime"].AsInt64,
                    EndTime = t["endTime"].AsInt64
                })
                .ToList();
        }
        public async Task<List<DBTaskNode>> GetDBTaskNodesByCids(List<string> cids)
        {
            return null;
        }
        public async Task SaveDBTaskNodes(string cid, List<DBTaskNode> newTasks)
        {
            // 获取现有任务ID（假设有缓存或查询）
            var filter = Builders<BsonDocument>.Filter.Eq("cid", cid);
            var existingTaskIds = GetExistingTaskIds(cid);
            var newTaskIds = newTasks.Select(t => t.TaskId).ToList();

            // 阶段1：清理废弃任务
            var taskIdsToRemove = existingTaskIds.Except(newTaskIds).ToList();
            if (taskIdsToRemove.Count > 0) // 有需要清理的任务时才执行
            {
                var pullFilter = Builders<BsonDocument>.Filter.Nin("taskId", newTaskIds);
                var pullUpdate = Builders<BsonDocument>.Update.PullFilter("tasks", pullFilter);
                await m_taskCollection.UpdateOneAsync(filter, pullUpdate).ConfigureAwait(false);
                // ConfigureAwait(false) 表示不需要同步上下文（这个上下文不是逻辑上下文，
                // 而是同步上下文：一种机制，用于在异步操作完成后，​​将代码的执行环境恢复到原始线程或特定逻辑上下文​​）
            }

            // 阶段2：更新现有任务
            var updates = new List<UpdateDefinition<BsonDocument>>();
            var arrayFilters = new List<ArrayFilterDefinition>();
            foreach (var task in newTasks.Where(t => existingTaskIds.Contains(t.TaskId)))
            {
                var placeholder = $"task{task.TaskId}";
                arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument($"{placeholder}.taskId", task.TaskId)));

                updates.Add(Builders<BsonDocument>.Update
                    .Set($"tasks.$[{placeholder}].state", task.State)
                    .Set($"tasks.$[{placeholder}].taskProgress", task.TaskProgress));
            }
            if (updates.Count > 0)
            {
                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
                await m_taskCollection.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Combine(updates), updateOptions).ConfigureAwait(false); 
            }

            // 阶段3：插入新任务
            var tasksToInsert = newTasks.Where(t => !existingTaskIds.Contains(t.TaskId)).ToList();
            if (tasksToInsert.Count > 0) // 确保有需要插入的任务
            {
                // 生成BSON文档列表（带校验）
                var newTaskDocuments = new List<BsonDocument>();
                foreach (var task in tasksToInsert)
                {
                    try
                    {
                        var doc = new BsonDocument
                        {
                            ["taskId"]          = task.TaskId,
                            ["state"]           = task.State,
                            ["taskProgress"]    = string.IsNullOrEmpty(task.TaskProgress)
                                ? BsonNull.Value
                                : (BsonValue)task.TaskProgress,
                            ["startTime"]       = task.StartTime,
                            ["endTime"]         = task.EndTime,
                            ["createTime"]      = DateTime.UtcNow,
                            ["version"]         = 1,
                        };
                        newTaskDocuments.Add(doc);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"任务数据生成失败 TaskID:{task.TaskId} : {ex.Message}");
                    }
                }
                var combinedUpdate = Builders<BsonDocument>.Update
                    .SetOnInsert("cid", cid) // 首次插入时设置cid,作用域只在IsUpsert = true时生效
                    .AddToSetEach("tasks", newTaskDocuments);
                await m_taskCollection.UpdateOneAsync(
                    filter,
                    combinedUpdate,
                    new UpdateOptions { IsUpsert = true } // 允许自动创建新文档
                ).ConfigureAwait(false);
            }
        }

        private List<int> GetExistingTaskIds(string cid)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("cid", cid);
            var projection = Builders<BsonDocument>.Projection      // 投影筛选结果用
                .Include("tasks.taskId")
                .Exclude("_id");
            var doc = m_taskCollection.Find(filter)
                .Project(projection)
                .FirstOrDefault();

            if (doc == null || !doc.Contains("tasks"))
                return new List<int>();

            var tasksArray = doc["tasks"].AsBsonArray;
            return tasksArray
                .Where(t => t.IsBsonDocument && t.AsBsonDocument.Contains("taskId"))
                .Select(t => t.AsBsonDocument["taskId"].AsInt32)
                .ToList();
        }
    }
}