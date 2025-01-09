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
        public async Task<List<DBWorldNode>> GetActiveWorldNodeAsync()
        {
            // 使用过滤器构建器创建查询条件
            var filter = Builders<BsonDocument>.Filter.Eq("isActive", true);

            // 查找满足条件的第一个文档
            var worldDocs = await m_worldCollection.Find(filter).ToListAsync();

            if (worldDocs != null)
            {
                List<DBWorldNode> worlds = new();
                foreach (var worldDoc in worldDocs)
                {
                    var wNode = new DBWorldNode();
                    wNode.WorldId = worldDoc["worldId"].ToInt32();
                    wNode.WorldName = worldDoc["worldName"].ToString();
                    wNode.WorldDesc = worldDoc["worldDesc"].ToString();
                    wNode.Status = worldDoc["status"].ToString();
                    wNode.CreatedAt = worldDoc["createdAt"].ToInt64();
                    wNode.MaxPlayers = worldDoc["maxPlayers"].ToInt32();
                    wNode.CreatedBy = worldDoc["createdBy"].ToString();
                    return worlds;
                }
            }
            return null;

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

        public async Task<bool> UpdatePasswordAsync(string uId, string newPassword)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var update = Builders<BsonDocument>.Update.Set("password", newPassword);

                // 执行更新操作
                var result = await m_worldCollection.UpdateOneAsync(filter, update);

                // 检查匹配和修改计数
                if (result.MatchedCount == 0)
                {
                    Console.WriteLine("No documents matched the filter.");
                    return false;
                }
                else if (result.ModifiedCount == 0)
                {
                    Console.WriteLine("Matched document(s) but no updates were made.");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during the update operation: {ex.Message}");
                return false;
            }
        }
        public async Task AddCharacterIdAsync(string uId, string characterId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var update = Builders<BsonDocument>.Update.AddToSet("characterIds", characterId);

                var result = await m_worldCollection.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                {
                    Console.WriteLine("No documents matched the filter.");
                }
                else if (result.ModifiedCount == 0)
                {
                    Console.WriteLine("Matched document(s) but no updates were made (characterId might already exist).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while adding the Character ID: {ex.Message}");
            }
        }
        public async Task DeleteCharacterIdAsync(string uId, string characterId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var update = Builders<BsonDocument>.Update.Pull("characterIds", characterId);

                var result = await m_worldCollection.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0)
                {
                    Console.WriteLine("No documents matched the filter.");
                }
                else if (result.ModifiedCount == 0)
                {
                    Console.WriteLine("Matched document(s) but no updates were made (characterId might not exist).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while removing the Character ID: {ex.Message}");
            }
        }
        public async Task<bool> DeleteUserByUidAsync(string uId)
        {
            var objectId = new ObjectId(uId);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            try
            {
                // 执行删除操作
                var deleteResult = await m_worldCollection.DeleteOneAsync(filter);
                await CharacterOperations.Instance.DeleteCharactersByUidAsync(uId);
                // 返回是否成功删除一条文档
                return deleteResult.DeletedCount > 0;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid ObjectId format: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }

        }
    }
}


