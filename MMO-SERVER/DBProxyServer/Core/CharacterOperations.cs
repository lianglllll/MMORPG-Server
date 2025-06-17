using MongoDB.Bson;
using MongoDB.Driver;
using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.Linq;
using Serilog;

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
                                foreach (var node in equippedSkills)
                                {
                                    var nodeDoc = node.AsBsonDocument;
                                    var sNode = new DBCharacterEquipSkillNode()
                                    {
                                        SkillId = nodeDoc["skillId"].AsInt32,
                                        Pos = nodeDoc["pos"].AsInt32
                                    };
                                    characterCombatNode.EquippedSkills.Add(sNode);
                                }
                                break;
                            case "chrTask":
                                var tasks = await TaskOperations.Instance.GetDBTaskNodesByCid(cNode.CId);
                                cNode.ChrTasks = new DBCharacterTasks();
                                cNode.ChrTasks.Tasks.Add(tasks);
                                break;
                            case "chrInventorys":
                                var Inventorys = await InventoryOperations.Instance.GetDBInventorysByCid(cNode.CId);
                                cNode.ChrInventorys = Inventorys;
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
                                    foreach (var node in equippedSkills)
                                    {
                                        var nodeDoc = node.AsBsonDocument;
                                        var sNode = new DBCharacterEquipSkillNode()
                                        {
                                            SkillId = nodeDoc["skillId"].AsInt32,
                                            Pos = nodeDoc["pos"].AsInt32
                                        };
                                        characterCombatNode.EquippedSkills.Add(sNode);
                                    }
                                    break;
                                case "chrTask":
                                    var tasks = await TaskOperations.Instance.GetDBTaskNodesByCid(cNode.CId);
                                    cNode.ChrTasks = new DBCharacterTasks();
                                    cNode.ChrTasks.Tasks.Add(tasks);
                                    break;
                                case "chrInventorys":
                                    var Inventorys = await InventoryOperations.Instance.GetDBInventorysByCid(cNode.CId);
                                    cNode.ChrInventorys = Inventorys;
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
        public async Task<string> AddCharacterAsync(DBCharacterNode cNode)
        {
            try
            {
                // Main Character Document
                ObjectId objectId = ObjectId.GenerateNewId();
                string cId = objectId.ToString();
                BsonDocument characterDocument = new BsonDocument
                {
                    { "_id", objectId },
                    { "uId", cNode.UId },
                    { "worldId", cNode.WorldId},
                    { "professionId", cNode.ProfessionId },
                    { "chrName", cNode.ChrName },
                    { "level", cNode.Level },
                    { "creationTimestamp", cNode.CreationTimestamp },
                };

                // Character Statistics
                if (cNode.ChrStatistics != null)
                {
                    BsonDocument characterStatistics = new BsonDocument
                    {
                        { "killCount", cNode.ChrStatistics.KillCount },
                        { "deathCount", cNode.ChrStatistics.DeathCount },   // 新增
                        { "taskCompleted", cNode.ChrStatistics.TaskCompleted } // 新增
                    };
                    characterDocument.Add("chrStatistics", characterStatistics);
                }

                // Character Status
                if(cNode.ChrStatus != null)
                {
                    BsonDocument characterStatus = new BsonDocument
                    {
                        { "hp", cNode.ChrStatus.Hp },
                        { "mp", cNode.ChrStatus.Mp },
                        { "exp", cNode.ChrStatus.Exp },
                        { "curSceneId", cNode.ChrStatus.CurSceneId },
                        { "x", cNode.ChrStatus.X },
                        { "y", cNode.ChrStatus.Y },
                        { "z", cNode.ChrStatus.Z }
                    };
                    characterDocument.Add("chrStatus", characterStatus);
                }

                // Character Assets
                if (cNode.ChrAssets != null)
                {
                    BsonDocument characterAssets = new BsonDocument
                    {
                        { "currency", new BsonDocument(cNode.ChrAssets.Currency) },       // 将货币映射为BsonDocument
                        { "achievements", new BsonArray(cNode.ChrAssets.Achievements) },  // 将成就列表转为BsonArray
                        { "titles", new BsonArray(cNode.ChrAssets.Titles) }               // 将头衔列表转为BsonArray
                    };
                    characterDocument.Add("chrAssets", characterAssets);
                }

                // Character Social
                if (cNode.ChrSocial != null)
                {
                    BsonDocument characterSocial = new BsonDocument
                    {
                        { "guildId", cNode.ChrSocial.GuildId },
                        { "faction", cNode.ChrSocial.Faction },
                        { "friendsList", new BsonArray(cNode.ChrSocial.FriendsList) } // 将好友列表转为BsonArray
                    };
                    characterDocument.Add("chrSocial", characterSocial);
                }

                if (cNode.ChrCombat != null)
                {
                    // 将技能列表转换为 BsonArray
                    BsonArray skillsArray = new BsonArray();
                    foreach (var skill in cNode.ChrCombat.Skills)
                    {
                        BsonDocument skillDocument = new BsonDocument
                        {
                            { "skillId", skill.SkillId },
                            { "level", skill.Level }
                        };
                        skillsArray.Add(skillDocument);
                    }


                    // 将装备了的技能ID列表转换为 BsonArray
                    BsonArray equipSkillsArray = new BsonArray();
                    foreach (var skill in cNode.ChrCombat.EquippedSkills)
                    {
                        BsonDocument skillDocument = new BsonDocument
                        {
                            { "skillId", skill.SkillId },
                            { "pos", skill.Pos }
                        };
                        equipSkillsArray.Add(skillDocument);
                    }

                    // 创建 characterCombatNode BsonDocument
                    BsonDocument characterCombatNode = new BsonDocument
                    {
                        { "skills", skillsArray },
                        { "equippedSkills", equipSkillsArray }
                    };

                    // 将 characterCombatNode 添加到 characterDocument
                    characterDocument.Add("chrCombat", characterCombatNode);
                }

                // 任务模块
                if (cNode.ChrTasks != null && cNode.ChrTasks.Tasks != null)
                {
                    await TaskOperations.Instance.SaveDBTaskNodes(cId, cNode.ChrTasks.Tasks.ToList());
                }
                
                // 背包
                if(cNode.ChrInventorys != null)
                {
                    await InventoryOperations.Instance.SaveDBInventorys(cId, cNode.ChrInventorys);
                }

                await m_characterCollection.InsertOneAsync(characterDocument);
                await UserOperations.Instance.AddCharacterIdAsync(cNode.UId, cNode.WorldId, cId);
                return cId;
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
                // 删除和User表的关联
                await UserOperations.Instance.RemoveCharacterIdAsync(chr["uId"].ToString(), chr["worldId"].ToInt32(), cId);
                // 删除和Task表的关联
                await TaskOperations.Instance.RemoveTasksByCid(cId);
                // 删除和Item表的关联
                await InventoryOperations.Instance.RemoveDBInventorysByCid(cId);

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
        public async Task<bool> SaveCharacterAsync(DBCharacterNode cNode)
        {
            try
            {
                // 构建基础过滤器
                ObjectId objectId = new ObjectId(cNode.CId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                // 动态构建更新操作
                var updateDefinitions = new List<UpdateDefinition<BsonDocument>>();

                // 基础字段更新
                var baseUpdate = Builders<BsonDocument>.Update
                    //.Set("uId", cNode.UId)
                    //.Set("professionId", cNode.ProfessionId)
                    .Set("chrName", cNode.ChrName)
                    .Set("level", cNode.Level);
                    //.Set("creationTimestamp", cNode.CreationTimestamp);
                updateDefinitions.Add(baseUpdate);

                // 统计信息更新
                if (cNode.ChrStatistics != null)
                {
                    var statsUpdate = Builders<BsonDocument>.Update.Set("chrStatistics", new BsonDocument
                    {
                        ["killCount"] = cNode.ChrStatistics.KillCount,
                        ["deathCount"] = cNode.ChrStatistics.DeathCount,
                        ["taskCompleted"] = cNode.ChrStatistics.TaskCompleted
                    });
                    updateDefinitions.Add(statsUpdate);
                }

                // 状态信息更新
                if (cNode.ChrStatus != null)
                {
                    var statusUpdate = Builders<BsonDocument>.Update.Set("chrStatus", new BsonDocument
                    {
                        ["hp"] = cNode.ChrStatus.Hp,
                        ["mp"] = cNode.ChrStatus.Mp,
                        ["exp"] = cNode.ChrStatus.Exp,
                        ["curSceneId"] = cNode.ChrStatus.CurSceneId,
                        ["x"] = cNode.ChrStatus.X,
                        ["y"] = cNode.ChrStatus.Y,
                        ["z"] = cNode.ChrStatus.Z
                    });
                    updateDefinitions.Add(statusUpdate);
                }

                // 资产信息更新
                if (cNode.ChrAssets != null)
                {
                    var assetsDoc = new BsonDocument
                    {
                        ["currency"] = new BsonDocument(cNode.ChrAssets.Currency),
                        ["achievements"] = new BsonArray(cNode.ChrAssets.Achievements),
                        ["titles"] = new BsonArray(cNode.ChrAssets.Titles)
                    };
                    updateDefinitions.Add(Builders<BsonDocument>.Update.Set("chrAssets", assetsDoc));
                }

                // 社交信息更新
                if (cNode.ChrSocial != null)
                {
                    var socialUpdate = Builders<BsonDocument>.Update.Set("chrSocial", new BsonDocument
                    {
                        ["guildId"] = cNode.ChrSocial.GuildId,
                        ["faction"] = cNode.ChrSocial.Faction,
                        ["friendsList"] = new BsonArray(cNode.ChrSocial.FriendsList)
                    });
                    updateDefinitions.Add(socialUpdate);
                }

                // 战斗信息更新
                if (cNode.ChrCombat != null)
                {
                    var combatDoc = new BsonDocument();

                    // 技能列表
                    var skillsArray = new BsonArray();
                    foreach (var skill in cNode.ChrCombat.Skills)
                    {
                        skillsArray.Add(new BsonDocument
                        {
                            ["skillId"] = skill.SkillId,
                            ["level"] = skill.Level
                        });
                    }
                    combatDoc["skills"] = skillsArray;

                    // 已装备技能
                    var equippedArray = new BsonArray();
                    foreach (var equip in cNode.ChrCombat.EquippedSkills)
                    {
                        equippedArray.Add(new BsonDocument
                        {
                            ["skillId"] = equip.SkillId,
                            ["pos"] = equip.Pos
                        });
                    }
                    combatDoc["equippedSkills"] = equippedArray;

                    updateDefinitions.Add(Builders<BsonDocument>.Update.Set("chrCombat", combatDoc));
                }

                // 任务模块
                if(cNode.ChrTasks != null && cNode.ChrTasks.Tasks != null)
                {
                    await TaskOperations.Instance.SaveDBTaskNodes(cNode.CId, cNode.ChrTasks.Tasks.ToList());
                }

                // 背包
                if (cNode.ChrInventorys != null)
                {
                    await InventoryOperations.Instance.SaveDBInventorys(cNode.CId, cNode.ChrInventorys);
                }
                // 合并所有更新操作
                var combinedUpdate = Builders<BsonDocument>.Update.Combine(updateDefinitions);

                // 执行原子更新
                var result = await m_characterCollection.UpdateOneAsync(
                    filter,
                    combinedUpdate,
                    new UpdateOptions { IsUpsert = false } // 禁止自动创建
                ).ConfigureAwait(false);

                // 返回更新是否生效
                return result.IsModifiedCountAvailable && result.ModifiedCount > 0;
            }
            catch (FormatException ex)
            {
                Log.Error($"无效的角色ID格式: {cNode.CId} - {ex.Message}");
                return false;
            }
            catch (MongoException ex)
            {
                Log.Error($"数据库操作失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"未知错误: {ex}");
                return false;
            }
        }

        #region 未使用
        public async Task<bool> RemoveCharactersByUidAsync(string uId)
        {
            try
            {
                // 定义过滤器以查找包含该 uId 的文档
                var filter = Builders<BsonDocument>.Filter.Eq("uId", uId);
                // 执行批量删除操作
                var deleteResult = await m_characterCollection.DeleteManyAsync(filter);
                // todo task也需要删除
                // todo Item也需要删除


                // 返回是否成功删除至少一条文档
                return deleteResult.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}
