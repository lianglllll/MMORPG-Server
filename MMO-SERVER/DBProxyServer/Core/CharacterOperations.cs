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

        public async Task<DBCharacterNode> GetCharacterByCidAsync(string cId)
        {
            try
            {
                ObjectId objectId = new ObjectId(cId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var chr = await _characterCollection.Find(filter).FirstOrDefaultAsync();
                if (chr == null)
                {
                    return null;
                }

                DBCharacterNode cNode = new();
                DBCharacterStatisticsNode characterStatisticsNode = new();
                DBCharacterStatusNode characterStatusNode = new();
                DBCharacterAssetsNode characterAssetsNode = new();

                // Assign nodes to cNode
                cNode.ChrStatistics = characterStatisticsNode;
                cNode.ChrStatus = characterStatusNode;
                cNode.ChrAssets = characterAssetsNode;

                // Map basic fields
                cNode.CId = cId;
                cNode.UId = chr["uId"].ToString();
                cNode.ProfessionId = chr["professionId"].ToInt32();
                cNode.ChrName = chr["chrName"].ToString();

                // Map statistics
                characterStatisticsNode.KillCount = chr["chrStatistics"]["killCount"].ToInt32();

                // Map status
                characterStatusNode.Hp = chr["chrStatus"]["hp"].ToInt32();
                characterStatusNode.Mp = chr["chrStatus"]["mp"].ToInt32();
                characterStatusNode.Level = chr["chrStatus"]["level"].ToInt32();
                characterStatusNode.Exp = chr["chrStatus"]["exp"].ToInt32();
                characterStatusNode.CurSpaceId = chr["chrStatus"]["curSpaceId"].ToInt32();
                characterStatusNode.X = chr["chrStatus"]["x"].ToInt32();
                characterStatusNode.Y = chr["chrStatus"]["y"].ToInt32();
                characterStatusNode.Z = chr["chrStatus"]["z"].ToInt32();

                // Map assets
                characterAssetsNode.BackpackData = ByteString.CopyFrom(chr["chrAssets"]["backpackData"].AsBsonBinaryData.Bytes);
                characterAssetsNode.EquipsData = ByteString.CopyFrom(chr["chrAssets"]["equipsData"].AsBsonBinaryData.Bytes);

                // Assuming Currency, Achievements, and Titles are collections in the chr document:
                var currencyData = chr["chrAssets"]["currency"].AsBsonDocument.ToDictionary(k => k.Name, v => v.Value.ToInt32());
                var achievementsData = chr["chrAssets"]["achievements"].AsBsonArray.Select(x => x.ToString()).ToList();
                var titlesData = chr["chrAssets"]["titles"].AsBsonArray.Select(x => x.ToString()).ToList();

                // 为 map 类型字段赋值
                foreach (var entry in currencyData)
                {
                    characterAssetsNode.Currency.Add(entry.Key, entry.Value);
                }

                // 为 repeated 类型字段赋值
                characterAssetsNode.Achievements.AddRange(achievementsData);
                characterAssetsNode.Titles.AddRange(titlesData);

                return cNode;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid ObjectId format: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving document: {ex.Message}");
                return null;
            }
        }
        public async Task<string> AddCharacterAsync(DBCharacterNode chrNode)
        {
            try
            {
                // Character Statistics
                BsonDocument characterStatistics = new BsonDocument
                {
                    { "killCount", chrNode.ChrStatistics.KillCount },
                    { "deathCount", chrNode.ChrStatistics.DeathCount },   // 新增
                    { "taskCompleted", chrNode.ChrStatistics.TaskCompleted } // 新增
                };

                // Character Status
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

                // Character Assets
                BsonDocument characterAssets = new BsonDocument
                {
                    { "backpackData", new BsonBinaryData(chrNode.ChrAssets.BackpackData.ToByteArray()) },
                    { "equipsData", new BsonBinaryData(chrNode.ChrAssets.EquipsData.ToByteArray()) },
                    { "currency", new BsonDocument(chrNode.ChrAssets.Currency) },       // 将货币映射为BsonDocument
                    { "achievements", new BsonArray(chrNode.ChrAssets.Achievements) },  // 将成就列表转为BsonArray
                    { "titles", new BsonArray(chrNode.ChrAssets.Titles) }               // 将头衔列表转为BsonArray
                };

                // Character Social
                BsonDocument characterSocial = new BsonDocument
                {
                    { "guildId", chrNode.ChrSocial.GuildId },
                    { "faction", chrNode.ChrSocial.Faction },
                    { "friendsList", new BsonArray(chrNode.ChrSocial.FriendsList) } // 将好友列表转为BsonArray
                };

                // Main Character Document
                ObjectId objectId = ObjectId.GenerateNewId();
                BsonDocument characterDocument = new BsonDocument
                {
                    { "_id", objectId },
                    { "cId", chrNode.CId },  // 新增
                    { "uId", chrNode.UId },
                    { "professionId", chrNode.ProfessionId },
                    { "chrName", chrNode.ChrName },
                    { "chrStatistics", characterStatistics },
                    { "chrStatus", characterStatus },
                    { "chrAssets", characterAssets },
                    { "chrSocial", characterSocial },  // 新增
                    { "creationTimestamp", chrNode.CreationTimestamp } // 新增
                };

                await _characterCollection.InsertOneAsync(characterDocument);

                return objectId.ToString();
            }
            catch (Exception ex)
            {
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

    }
}
