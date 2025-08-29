using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBUser;
using HS.Protobuf.DBProxy.DBWorld;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBProxyServer.Core
{
    public class WorldOperations: Singleton<WorldOperations>
    {
        private IMongoCollection<BsonDocument> m_worldCollection;

        public void  Init(MongoDBConnection dbConnection)
        {
            m_worldCollection = dbConnection.GetCollection<BsonDocument>("world");
        }
        public async Task<DBWorldNode> GetDBWorldNodeByWorldIdAsync(int worldId)
        {
            // 使用过滤器构建器创建查询条件
            var filter = Builders<BsonDocument>.Filter.Eq("worldId", worldId);

            // 查找满足条件的第一个文档
            var worldDoc = await m_worldCollection.Find(filter).FirstOrDefaultAsync();

            if (worldDoc != null)
            {
                DBWorldNode wNode = new();
                wNode.WorldId = worldId;
                wNode.WorldName = worldDoc["worldName"].ToString();
                wNode.WorldDesc = worldDoc["worldDesc"].ToString();
                wNode.Status = worldDoc["status"].ToString();
                wNode.CreatedAt = worldDoc["createdAt"].ToInt64();
                wNode.MaxPlayers = worldDoc["maxPlayers"].ToInt32();
                wNode.CreatedBy = worldDoc["createdBy"].ToString();

                return wNode; 
            }

            return null;
        }
        public async Task<List<DBWorldNode>> GetAllWorldNodeAsync()
        {
            // 获取所有文档，不使用过滤器
            var worldDocs = await m_worldCollection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();

            if (worldDocs != null)
            {
                List<DBWorldNode> worlds = new();
                foreach (var worldDoc in worldDocs)
                {
                    var wNode = new DBWorldNode
                    {
                        WorldId = worldDoc["worldId"].ToInt32(),
                        WorldName = worldDoc["worldName"].ToString(),
                        WorldDesc = worldDoc["worldDesc"].ToString(),
                        Status = worldDoc["status"].ToString(),
                        CreatedAt = worldDoc["createdAt"].ToInt64(),
                        MaxPlayers = worldDoc["maxPlayers"].ToInt32(),
                        CreatedBy = worldDoc["createdBy"].ToString()
                    };
                    worlds.Add(wNode);
                }
                return worlds;
            }

            // 如果没有文档，返回空列表
            return new List<DBWorldNode>();
        }
        public async Task<bool> AddWorldAsync(DBWorldNode node)
        {
            try
            {
                BsonDocument worldDoc = new BsonDocument
                {
                    { "worldId", node.WorldId },
                    { "worldName", node.WorldName },
                    { "worldDesc", node.WorldDesc },
                    { "status", node.Status },
                    { "createdAt", node.CreatedAt },
                    { "maxPlayers", node.MaxPlayers },
                    { "createdBy", node.CreatedBy },
                };

                await m_worldCollection.InsertOneAsync(worldDoc);
                return true; // 插入成功
            }
            catch (Exception ex)
            {
                // 可以根据需要记录日志或者处理特定的异常类型
                Console.WriteLine($"Error inserting document: {ex.Message}");
                return false; // 插入失败
            }
        }

    }
}


