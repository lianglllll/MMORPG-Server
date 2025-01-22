using MongoDB.Bson;
using MongoDB.Driver;
using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.Linq;

namespace DBProxyServer.Core
{
    public class CharacterOperations:Singleton<CharacterOperations>
    {
        private  IMongoCollection<BsonDocument>? m_characterCollection;

        public void Init(MongoDBConnection dbConnection)
        {
            m_characterCollection = dbConnection.GetCollection<BsonDocument>("character");
        }

        public async Task<DBCharacterNode> GetCharacterByCidAsync(string cId, FieldMask readMask)
        {
            try
            {
                ObjectId objectId = new ObjectId(cId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var chr = await m_characterCollection.Find(filter).FirstOrDefaultAsync();
                if (chr == null)
                {
                    goto End;
                }

                DBCharacterNode cNode = new();

                // Map basic fields
                cNode.CId = chr["_id"].AsObjectId.ToString(); 
                cNode.UId = chr["uId"].ToString();
                cNode.ProfessionId = chr["professionId"].ToInt32();
                cNode.ChrName = chr["chrName"].ToString();
                cNode.Level = chr["level"].ToInt32();
                cNode.CreationTimestamp = chr["creationTimestamp"].ToInt64();

                if (readMask != null)
                {
                    foreach (var path in readMask.Paths)
                    {
                        switch (path)
                        {
                            case "chrStatistics":
                                if (!chr.Contains("chrStatistics")) break;
                                DBCharacterStatisticsNode characterStatisticsNode = new();
                                cNode.ChrStatistics = characterStatisticsNode;
                                characterStatisticsNode.KillCount = chr["chrStatistics"]["killCount"].ToInt32();
                                characterStatisticsNode.DeathCount = chr["chrStatistics"]["deathCount"].ToInt32();
                                characterStatisticsNode.TaskCompleted = chr["chrStatistics"]["taskCompleted"].ToInt32();
                                break;
                            case "chrStatus":
                                if (!chr.Contains("chrStatus")) break;
                                DBCharacterStatusNode characterStatusNode = new();
                                cNode.ChrStatus = characterStatusNode;
                                characterStatusNode.Hp = chr["chrStatus"]["hp"].ToInt32();
                                characterStatusNode.Mp = chr["chrStatus"]["mp"].ToInt32();
                                characterStatusNode.Exp = chr["chrStatus"]["exp"].ToInt32();
                                characterStatusNode.CurSceneId = chr["chrStatus"]["curSceneId"].ToInt32();
                                characterStatusNode.X = chr["chrStatus"]["x"].ToInt32();
                                characterStatusNode.Y = chr["chrStatus"]["y"].ToInt32();
                                characterStatusNode.Z = chr["chrStatus"]["z"].ToInt32();
                                break;
                            case "chrAssets":
                                if (!chr.Contains("chrAssets")) break;
                                DBCharacterAssetsNode characterAssetsNode = new();
                                cNode.ChrAssets = characterAssetsNode;
                                characterAssetsNode.BackpackData = ByteString.CopyFrom(chr["chrAssets"]["backpackData"].AsBsonBinaryData.Bytes);
                                characterAssetsNode.EquipsData = ByteString.CopyFrom(chr["chrAssets"]["equipsData"].AsBsonBinaryData.Bytes);
                                var currencyData = chr["chrAssets"]["currency"].AsBsonDocument.ToDictionary(k => k.Name, v => v.Value.ToInt32());
                                foreach (var entry in currencyData)
                                {
                                    characterAssetsNode.Currency.Add(entry.Key, entry.Value);
                                }
                                var achievementsData = chr["chrAssets"]["achievements"].AsBsonArray.Select(x => x.ToString()).ToList();
                                characterAssetsNode.Achievements.AddRange(achievementsData);
                                var titlesData = chr["chrAssets"]["titles"].AsBsonArray.Select(x => x.ToString()).ToList();
                                characterAssetsNode.Titles.AddRange(titlesData);
                                break;
                            case "chrSocial":
                                if (!chr.Contains("chrSocial")) break;
                                DBCharacterSocialNode characterSocialNode = new();
                                cNode.ChrSocial = characterSocialNode;
                                characterSocialNode.GuildId = chr["chrSocial"]["guildId"].ToString();
                                characterSocialNode.Faction = chr["chrSocial"]["faction"].ToString();
                                var friendsData = chr["chrSocial"]["friendsList"].AsBsonArray.Select(x => x.ToString()).ToList();
                                characterSocialNode.FriendsList.AddRange(friendsData);
                                break;
                            case "chrCombat":
                                if (!chr.Contains("chrCombat")) break;
                                var characterCombatNode = new DBCharacterCombatNode();
                                cNode.ChrCombat = characterCombatNode;

                                var chrCombat = chr["chrCombat"].AsBsonDocument;

                                var skills = chrCombat["skills"].AsBsonArray;
                                foreach (var skill in skills)
                                {
                                    var skillDoc = skill.AsBsonDocument;
                                    var skillNode = new DBCharacterSkillNode
                                    {
                                        SkillId = skillDoc["skillId"].AsInt32,
                                        Level = skillDoc["level"].AsInt32
                                    };
                                    characterCombatNode.Skills.Add(skillNode);
                                }

                                var equippedSkills = chrCombat["equippedSkills"].AsBsonArray;
                                foreach (var skillId in equippedSkills)
                                {
                                    characterCombatNode.EquippedSkills.Add(skillId.AsInt32);
                                }
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown field: {path}");
                        }
                    }
                }

                return cNode;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid ObjectId format: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving document: {ex.Message}");
            }
         End:
            return null;
        }
        public async Task<List<DBCharacterNode>> GetCharactersByUWidAsync(string uId, int worldId,FieldMask readMask)
        {
            try
            {
                // 获取角色的 ObjectId 列表
                var chrIds = await UserOperations.Instance.GetCharacterIdsAsync(uId, worldId);
                if (chrIds.Count == 0)
                {
                    goto End;
                }

                // 将字符串ID转换为ObjectId列表
                var objectIdList = chrIds.Select(id => new ObjectId(id)).ToList();

                // 使用 $in 操作符创建过滤器
                var filter = Builders<BsonDocument>.Filter.In("_id", objectIdList);

                // 可以根据具体需求使用 readMask 定制投影
                // var projection = Builders<BsonDocument>.Projection.Include(readMask.Paths);
                // var projection = projectionBuilder.Exclude("_id"); // 假设你不想返回 _id

                // 查询角色集合
                //var characterDocuments = await m_characterCollection.Find(filter).Project<DBCharacterNode>(projection).ToListAsync();
                var chrs = await m_characterCollection.Find(filter).ToListAsync();

                List<DBCharacterNode> cNodes = new();
                foreach(var chr in chrs)
                {
                    DBCharacterNode cNode = new();

                    // Map basic fields
                    cNode.CId = chr["_id"].AsObjectId.ToString(); ;
                    cNode.UId = chr["uId"].ToString();
                    cNode.ProfessionId = chr["professionId"].ToInt32();
                    cNode.ChrName = chr["chrName"].ToString();
                    cNode.Level = chr["level"].ToInt32();
                    cNode.CreationTimestamp = chr["creationTimestamp"].ToInt64();

                    if(readMask != null)
                    {
                        foreach (var path in readMask.Paths)
                        {
                            switch (path)
                            {
                                case "chrStatistics":
                                    DBCharacterStatisticsNode characterStatisticsNode = new();
                                    cNode.ChrStatistics = characterStatisticsNode;
                                    characterStatisticsNode.KillCount = chr["chrStatistics"]["killCount"].ToInt32();
                                    break;
                                case "chrStatus":
                                    DBCharacterStatusNode characterStatusNode = new();
                                    cNode.ChrStatus = characterStatusNode;
                                    characterStatusNode.Hp = chr["chrStatus"]["hp"].ToInt32();
                                    characterStatusNode.Mp = chr["chrStatus"]["mp"].ToInt32();
                                    characterStatusNode.Exp = chr["chrStatus"]["exp"].ToInt32();
                                    characterStatusNode.CurSceneId = chr["chrStatus"]["curSceneId"].ToInt32();
                                    characterStatusNode.X = chr["chrStatus"]["x"].ToInt32();
                                    characterStatusNode.Y = chr["chrStatus"]["y"].ToInt32();
                                    characterStatusNode.Z = chr["chrStatus"]["z"].ToInt32();
                                    break;
                                case "chrAssets":
                                    DBCharacterAssetsNode characterAssetsNode = new();
                                    cNode.ChrAssets = characterAssetsNode;
                                    characterAssetsNode.BackpackData = ByteString.CopyFrom(chr["chrAssets"]["backpackData"].AsBsonBinaryData.Bytes);
                                    characterAssetsNode.EquipsData = ByteString.CopyFrom(chr["chrAssets"]["equipsData"].AsBsonBinaryData.Bytes);
                                    var currencyData = chr["chrAssets"]["currency"].AsBsonDocument.ToDictionary(k => k.Name, v => v.Value.ToInt32());
                                    foreach (var entry in currencyData)
                                    {
                                        characterAssetsNode.Currency.Add(entry.Key, entry.Value);
                                    }
                                    var achievementsData = chr["chrAssets"]["achievements"].AsBsonArray.Select(x => x.ToString()).ToList();
                                    characterAssetsNode.Achievements.AddRange(achievementsData);
                                    var titlesData = chr["chrAssets"]["titles"].AsBsonArray.Select(x => x.ToString()).ToList();
                                    characterAssetsNode.Titles.AddRange(titlesData);
                                    break;
                                case "chrSocial":
                                    DBCharacterSocialNode characterSocialNode = new();
                                    cNode.ChrSocial = characterSocialNode;
                                    characterSocialNode.GuildId = chr["chrSocial"]["guildId"].ToString();
                                    characterSocialNode.Faction = chr["chrSocial"]["faction"].ToString();
                                    var friendsData = chr["chrSocial"]["friendsList"].AsBsonArray.Select(x => x.ToString()).ToList();
                                    characterSocialNode.FriendsList.AddRange(friendsData);
                                    break;
                                case "chrCombat":
                                    var characterCombatNode = new DBCharacterCombatNode();
                                    cNode.ChrCombat = characterCombatNode;

                                    var chrCombat = chr["chrCombat"].AsBsonDocument;

                                    var skills = chrCombat["skills"].AsBsonArray;
                                    foreach (var skill in skills)
                                    {
                                        var skillDoc = skill.AsBsonDocument;
                                        var skillNode = new DBCharacterSkillNode
                                        {
                                            SkillId = skillDoc["skillId"].AsInt32,
                                            Level = skillDoc["level"].AsInt32
                                        };
                                        characterCombatNode.Skills.Add(skillNode);
                                    }

                                    var equippedSkills = chrCombat["equippedSkills"].AsBsonArray;
                                    foreach (var skillId in equippedSkills)
                                    {
                                        characterCombatNode.EquippedSkills.Add(skillId.AsInt32);
                                    }

                                    break;
                                default:
                                    throw new InvalidOperationException($"Unknown field: {path}");
                            }
                        }
                    }
                    cNodes.Add(cNode);
                }
                return cNodes;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Invalid ObjectId format: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving document: {ex.Message}");
            }
        End:
            return null;
        }
        public async Task<string> AddCharacterAsync(DBCharacterNode chrNode)
        {
            try
            {
                // Main Character Document
                ObjectId objectId = ObjectId.GenerateNewId();
                string objectIdStr = objectId.ToString();
                BsonDocument characterDocument = new BsonDocument
                {
                    { "_id", objectId },
                    { "uId", chrNode.UId },
                    { "worldId", chrNode.WorldId},
                    { "professionId", chrNode.ProfessionId },
                    { "chrName", chrNode.ChrName },
                    { "level", chrNode.Level },
                    { "creationTimestamp", chrNode.CreationTimestamp },
                };

                // Character Statistics
                if (chrNode.ChrStatistics != null)
                {
                    BsonDocument characterStatistics = new BsonDocument
                    {
                        { "killCount", chrNode.ChrStatistics.KillCount },
                        { "deathCount", chrNode.ChrStatistics.DeathCount },   // 新增
                        { "taskCompleted", chrNode.ChrStatistics.TaskCompleted } // 新增
                    };
                    characterDocument.Add("chrStatistics", characterStatistics);
                }

                // Character Status
                if(chrNode.ChrStatus != null)
                {
                    BsonDocument characterStatus = new BsonDocument
                    {
                        { "hp", chrNode.ChrStatus.Hp },
                        { "mp", chrNode.ChrStatus.Mp },
                        { "exp", chrNode.ChrStatus.Exp },
                        { "curSceneId", chrNode.ChrStatus.CurSceneId },
                        { "x", chrNode.ChrStatus.X },
                        { "y", chrNode.ChrStatus.Y },
                        { "z", chrNode.ChrStatus.Z }
                    };
                    characterDocument.Add("chrStatus", characterStatus);
                }

                // Character Assets
                if (chrNode.ChrAssets != null)
                {
                    BsonDocument characterAssets = new BsonDocument
                    {
                        { "backpackData", new BsonBinaryData(chrNode.ChrAssets.BackpackData.ToByteArray()) },
                        { "equipsData", new BsonBinaryData(chrNode.ChrAssets.EquipsData.ToByteArray()) },
                        { "currency", new BsonDocument(chrNode.ChrAssets.Currency) },       // 将货币映射为BsonDocument
                        { "achievements", new BsonArray(chrNode.ChrAssets.Achievements) },  // 将成就列表转为BsonArray
                        { "titles", new BsonArray(chrNode.ChrAssets.Titles) }               // 将头衔列表转为BsonArray
                    };
                    characterDocument.Add("chrAssets", characterAssets);
                }

                // Character Social
                if (chrNode.ChrSocial != null)
                {
                    BsonDocument characterSocial = new BsonDocument
                    {
                        { "guildId", chrNode.ChrSocial.GuildId },
                        { "faction", chrNode.ChrSocial.Faction },
                        { "friendsList", new BsonArray(chrNode.ChrSocial.FriendsList) } // 将好友列表转为BsonArray
                    };
                    characterDocument.Add("chrSocial", characterSocial);
                }

                if (chrNode.ChrCombat != null)
                {
                    // 将技能列表转换为 BsonArray
                    BsonArray skillsArray = new BsonArray();
                    foreach (var skill in chrNode.ChrCombat.Skills)
                    {
                        BsonDocument skillDocument = new BsonDocument
                        {
                            { "skillId", skill.SkillId },
                            { "level", skill.Level }
                        };
                        skillsArray.Add(skillDocument);
                    }

                    // 将装备技能ID列表转换为 BsonArray
                    BsonArray equippedSkillsArray = new BsonArray(chrNode.ChrCombat.EquippedSkills);

                    // 创建 characterCombatNode BsonDocument
                    BsonDocument characterCombatNode = new BsonDocument
                    {
                        { "skills", skillsArray },
                        { "equippedSkills", equippedSkillsArray }
                    };

                    // 将 characterCombatNode 添加到 characterDocument
                    characterDocument.Add("chrCombat", characterCombatNode);
                }

                await m_characterCollection.InsertOneAsync(characterDocument);
                await UserOperations.Instance.AddCharacterIdAsync(chrNode.UId, chrNode.WorldId, objectIdStr);
                return objectIdStr;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting document: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> RemoveCharacterByCidAsync(string cId)
        {
            // 定义过滤器以查找包含该cid的文档
            var objectId = new ObjectId(cId);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            try
            {
                // 删除user终的cid
                var chr = await m_characterCollection.Find(filter).FirstOrDefaultAsync();
                await UserOperations.Instance.RemoveCharacterIdAsync(chr["uId"].ToString(), chr["worldId"].ToInt32(), cId);
                
                // 执行删除操作
                var deleteResult = await m_characterCollection.DeleteOneAsync(filter);
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
        public async Task<bool> RemoveCharactersByUidAsync(string uId)
        {
            try
            {
                // 定义过滤器以查找包含该 uId 的文档
                var filter = Builders<BsonDocument>.Filter.Eq("uId", uId);

                // 执行批量删除操作
                var deleteResult = await m_characterCollection.DeleteManyAsync(filter);

                // 返回是否成功删除至少一条文档
                return deleteResult.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }

        }
        public async Task<bool> CheckCharacterNameExistenceAsync(string cName)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("chrName", cName);
                var count = await m_characterCollection.CountDocumentsAsync(filter);
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking character name existence: {ex.Message}");
                // 在发生异常时，返回 false 或根据需求处理错误逻辑
                return false;
            }
        }
    }
}
