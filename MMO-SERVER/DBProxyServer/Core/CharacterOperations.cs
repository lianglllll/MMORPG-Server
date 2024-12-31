using MongoDB.Bson;
using MongoDB.Driver;
using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using Google.Protobuf;

namespace DBProxyServer.Core
{
    public class CharacterOperations:Singleton<CharacterOperations>
    {
        private  IMongoCollection<BsonDocument> _characterCollection;

        public void Init(MongoDBConnection dbConnection)
        {
            _characterCollection = dbConnection.GetCollection<BsonDocument>("character");
        }

        public async Task<string> AddCharacterAsync(DBCharacterNode chrNode)
        {
            try
            {
                BsonDocument characterStatistics = new BsonDocument
                {
                    { "killCount", chrNode.ChrStatistics.KillCount }
                    // 添加其他统计数据字段
                };

                BsonDocument characterStatus = new BsonDocument
                {
                    { "hp", chrNode.ChrStatus.Hp },
                    { "mp", chrNode.ChrStatus.Mp },
                    { "level", chrNode.ChrStatus.Level },
                    { "exp", chrNode.ChrStatus.Exp },
                    { "curSpaceId", chrNode.ChrStatus.CurSpaceId },
                    { "x", chrNode.ChrStatus.X },
                    { "y", chrNode.ChrStatus.Y },
                    { "z", chrNode.ChrStatus.Z }
                };

                BsonDocument characterAssets = new BsonDocument
                {
                    { "backpackData", new BsonBinaryData(chrNode.ChrAssets.BackpackData.ToByteArray()) },
                    { "equipsData", new BsonBinaryData(chrNode.ChrAssets.EquipsData.ToByteArray()) }
                };

                ObjectId objectId = ObjectId.GenerateNewId();
                BsonDocument characterDocument = new BsonDocument
                {
                    { "_id", objectId },  
                    { "uId", chrNode.UId },
                    { "professionId", chrNode.ProfessionId },
                    { "chrName", chrNode.ChrName },
                    { "chrStatistics", characterStatistics },
                    { "chrStatus", characterStatus },
                    { "chrAssets", characterAssets }
                };

                await _characterCollection.InsertOneAsync(characterDocument);

                // 也需要给user表中进行更新
                await UserOperations.Instance.AddCharacterIdAsync(chrNode.UId, objectId.ToString());

                return objectId.ToString();
            }
            catch (Exception ex)
            {
                // 处理插入操作中的异常
                Console.WriteLine($"Error inserting document: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> DeleteCharacterByCidAsync(string cId)
        {
            // 定义过滤器以查找包含该cid的文档
            var objectId = new ObjectId(cId);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            try
            {
                // 删除user终的cid
                var chr = await _characterCollection.Find(filter).FirstOrDefaultAsync();
                await UserOperations.Instance.DeleteCharacterIdAsync(chr["uId"].ToString(),cId);
                
                // 执行删除操作
                var deleteResult = await _characterCollection.DeleteOneAsync(filter);
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
        public async Task<bool> DeleteCharactersByUidAsync(string uId)
        {
            try
            {
                // 定义过滤器以查找包含该 uId 的文档
                var filter = Builders<BsonDocument>.Filter.Eq("uId", uId);

                // 执行批量删除操作
                var deleteResult = await _characterCollection.DeleteManyAsync(filter);

                // 返回是否成功删除至少一条文档
                return deleteResult.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }

        }
        public async Task<DBCharacterNode> GetCharacterByCidAsync(string cId)
        {
            try
            {
                ObjectId objectId = new ObjectId(cId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var chr = await _characterCollection.Find(filter).FirstOrDefaultAsync();
                if(chr == null)
                {
                    return null;
                }

                DBCharacterNode cNode = new();
                DBCharacterStatisticsNode characterStatisticsNode = new();
                DBCharacterStatusNode characterStatusNode = new();
                DBCharacterAssetsNode characterAssetsNode = new();

                cNode.ChrStatistics = characterStatisticsNode;
                cNode.ChrStatus = characterStatusNode;
                cNode.ChrAssets = characterAssetsNode;

                cNode.CId = cId;
                cNode.UId = chr["uId"].ToString();
                cNode.ProfessionId = chr["professionId"].ToInt32();
                cNode.ChrName = chr["chrName"].ToString();

                characterStatisticsNode.KillCount = chr["chrStatistics"]["killCount"].ToInt32();

                characterStatusNode.Hp = chr["chrStatus"]["hp"].ToInt32();
                characterStatusNode.Mp = chr["chrStatus"]["mp"].ToInt32();
                characterStatusNode.Level = chr["chrStatus"]["level"].ToInt32();
                characterStatusNode.Exp = chr["chrStatus"]["exp"].ToInt32();
                characterStatusNode.CurSpaceId = chr["chrStatus"]["curSpaceId"].ToInt32();
                characterStatusNode.X = chr["chrStatus"]["x"].ToInt32();
                characterStatusNode.Y = chr["chrStatus"]["y"].ToInt32();
                characterStatusNode.Z = chr["chrStatus"]["z"].ToInt32();

                characterAssetsNode.BackpackData = ByteString.CopyFrom(chr["chrAssets"]["backpackData"].AsBsonBinaryData.Bytes);
                characterAssetsNode.EquipsData = ByteString.CopyFrom(chr["chrAssets"]["equipsData"].AsBsonBinaryData.Bytes);

                return cNode;
            }
            catch (FormatException ex)
            {
                // 处理无效的 ObjectId 格式异常
                Console.WriteLine($"Invalid ObjectId format: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // 处理其他可能的异常
                Console.WriteLine($"Error retrieving document: {ex.Message}");
                return null;
            }
        }

    }
}
