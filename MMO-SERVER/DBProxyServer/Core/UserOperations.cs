using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBUser;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DBProxyServer.Core
{
    public class UserOperations: Singleton<UserOperations>
    {
        private IMongoCollection<BsonDocument> m_userCollection;

        public void  Init(MongoDBConnection dbConnection)
        {
            m_userCollection = dbConnection.GetCollection<BsonDocument>("user");
        }
        public async Task<bool> AddUserAsync(DBUserNode dBUser)
        {
            try
            {
                var user = new BsonDocument
                {
                    { "userName", dBUser.UserName },
                    { "password", dBUser.Password },
                    { "email", dBUser.Email },
                    { "isEmailVerified", dBUser.IsEmailVerified },
                    { "creationTimestamp", dBUser.CreationTimestamp },
                    { "lastLoginTimestamp", dBUser.LastLoginTimestamp },
                    { "lastPasswordChangeTimestamp", dBUser.LastPasswordChangeTimestamp },
                    { "lockedUntilTimesTamp", dBUser.LockedUntilTimesTamp },
                    { "accessLevel", dBUser.AccessLevel },
                    { "accountStatus", dBUser.AccountStatus }
                };
                if (dBUser.WorldCharacters != null && dBUser.WorldCharacters.Count > 0)
                {
                    var worldCharactersDoc = new BsonDocument();
                    foreach (var entry in dBUser.WorldCharacters)
                    {
                        worldCharactersDoc.Add(entry.Key, new BsonArray(entry.Value.CharacterIds));
                    }
                    user.Add("worldCharacters", worldCharactersDoc);
                }
                if (dBUser.ActivityLogs != null && dBUser.ActivityLogs.Count > 0)
                {
                    user.Add("activityLogs", new BsonArray(dBUser.ActivityLogs));
                }
                if (dBUser.LinkedAccounts != null && dBUser.LinkedAccounts.Count > 0)
                {
                    var linkedAccountsArray = new BsonArray();
                    foreach (var kvp in dBUser.LinkedAccounts)
                    {
                        var linkedAccountDoc = new BsonDocument
                        {
                            { "Key", kvp.Key },
                            { "Value", kvp.Value }
                        };
                        linkedAccountsArray.Add(linkedAccountDoc);
                    }
                    user.Add("linkedAccounts", linkedAccountsArray);
                }
                if (dBUser.Preferences != null && dBUser.Preferences.Count > 0)
                {
                    var preferencesArray = new BsonArray();
                    foreach (var kvp in dBUser.Preferences)
                    {
                        var preferenceDoc = new BsonDocument
                        {
                            { "Key", kvp.Key },
                            { "Value", kvp.Value }
                        };
                        preferencesArray.Add(preferenceDoc);
                    }
                    user.Add("preferences", preferencesArray);
                }

                await m_userCollection.InsertOneAsync(user);
                return true; // 插入成功
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting document: {ex.Message}");
                return false; // 插入失败
            }
        }
        public async Task DeleteCharacterIdAsync(string uId, string characterId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var update = Builders<BsonDocument>.Update.Pull("characterIds", characterId);

                var result = await m_userCollection.UpdateOneAsync(filter, update);

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
        public async Task<bool> UpdatePasswordAsync(string uId, string newPassword)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var update = Builders<BsonDocument>.Update.Set("password", newPassword);

                // 执行更新操作
                var result = await m_userCollection.UpdateOneAsync(filter, update);

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
        public async Task<DBUserNode> GetDBUserByNameAsync(string name)
        {
            // 使用过滤器构建器创建查询条件
            var filter = Builders<BsonDocument>.Filter.Eq("userName", name);

            // 查找满足条件的第一个文档
            var userDocument = await m_userCollection.Find(filter).FirstOrDefaultAsync();

            if (userDocument != null)
            {
                DBUserNode dBUserNode = new();
                dBUserNode.UId = userDocument["_id"].AsObjectId.ToString();
                dBUserNode.UserName = userDocument["userName"].ToString();
                dBUserNode.Password = userDocument["password"].ToString();
                dBUserNode.Email = userDocument["email"].ToString();
                dBUserNode.IsEmailVerified = userDocument["isEmailVerified"].ToBoolean();
                dBUserNode.CreationTimestamp = userDocument["creationTimestamp"].ToInt64();
                dBUserNode.LastLoginTimestamp = userDocument["lastLoginTimestamp"].ToInt64();
                dBUserNode.LastPasswordChangeTimestamp = userDocument["lastPasswordChangeTimestamp"].ToInt64();
                dBUserNode.LockedUntilTimesTamp = userDocument["lockedUntilTimesTamp"].ToInt64();
                dBUserNode.AccessLevel = userDocument["accessLevel"].ToString();
                dBUserNode.AccountStatus = userDocument["accountStatus"].ToString();
                if (userDocument.Contains("activityLogs"))
                {
                    BsonArray activityLogs = userDocument["activityLogs"].AsBsonArray;
                    foreach (var activityLog in activityLogs)
                    {
                        dBUserNode.ActivityLogs.Add(activityLog.ToString());
                    }
                }
                if (userDocument.Contains("linkedAccounts"))
                {
                    BsonArray linkedAccounts = userDocument["linkedAccounts"].AsBsonArray;
                    foreach (var linkedAccount in linkedAccounts)
                    {
                        var linkedAccountDoc = linkedAccount.AsBsonDocument;
                        string key = linkedAccountDoc["Key"].AsString;
                        string value = linkedAccountDoc["Value"].AsString;
                        dBUserNode.LinkedAccounts.Add(key, value);
                    }
                }
                if (userDocument.Contains("preferences"))
                {
                    BsonArray preferences = userDocument["preferences"].AsBsonArray;
                    foreach (var preference in preferences)
                    {
                        var preferencesDoc = preferences.AsBsonDocument;
                        string key = preferencesDoc["Key"].AsString;
                        string value = preferencesDoc["Value"].AsString;
                        dBUserNode.Preferences.Add(key, value);
                    }
                }
                // dBUserNode.IsOnline = userDocument["isOnline"].ToBoolean();
                return dBUserNode;
            }

            return null;
        }
        public async Task<DBUserNode> GetDBUserByUidAsync(string uId)
        {
            var objectId = new ObjectId(uId);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

            // 查找满足条件的第一个文档
            var userDocument = await m_userCollection.Find(filter).FirstOrDefaultAsync();

            if (userDocument != null)
            {
                DBUserNode dBUserNode = new();
                dBUserNode.UId = userDocument["_id"].AsObjectId.ToString();
                dBUserNode.UserName = userDocument["userName"].ToString();
                dBUserNode.Password = userDocument["password"].ToString();
                dBUserNode.Email = userDocument["email"].ToString();
                dBUserNode.IsEmailVerified = userDocument["isEmailVerified"].ToBoolean();
                dBUserNode.CreationTimestamp = userDocument["creationTimestamp"].ToInt64();
                dBUserNode.LastLoginTimestamp = userDocument["lastLoginTimestamp"].ToInt64();
                dBUserNode.LastPasswordChangeTimestamp = userDocument["lastPasswordChangeTimestamp"].ToInt64();
                dBUserNode.LockedUntilTimesTamp = userDocument["lockedUntilTimesTamp"].ToInt64();
                dBUserNode.AccessLevel = userDocument["accessLevel"].ToString();
                dBUserNode.AccountStatus = userDocument["accountStatus"].ToString();
                if (userDocument.Contains("activityLogs"))
                {
                    BsonArray activityLogs = userDocument["activityLogs"].AsBsonArray;
                    foreach (var activityLog in activityLogs)
                    {
                        dBUserNode.ActivityLogs.Add(activityLog.ToString());
                    }
                }
                if (userDocument.Contains("linkedAccounts"))
                {
                    BsonArray linkedAccounts = userDocument["linkedAccounts"].AsBsonArray;
                    foreach (var linkedAccount in linkedAccounts)
                    {
                        var linkedAccountDoc = linkedAccount.AsBsonDocument;
                        string key = linkedAccountDoc["Key"].AsString;
                        string value = linkedAccountDoc["Value"].AsString;
                        dBUserNode.LinkedAccounts.Add(key, value);
                    }
                }
                if (userDocument.Contains("preferences"))
                {
                    BsonArray preferences = userDocument["preferences"].AsBsonArray;
                    foreach (var preference in preferences)
                    {
                        var preferencesDoc = preferences.AsBsonDocument;
                        string key = preferencesDoc["Key"].AsString;
                        string value = preferencesDoc["Value"].AsString;
                        dBUserNode.Preferences.Add(key, value);
                    }
                }
                //dBUserNode.IsOnline = userDocument["isOnline"].ToBoolean();

                return dBUserNode;
            }

            return null;
        }

        public async Task<bool> AddCharacterIdAsync(string uId, int worldId, string characterId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                // Use $addToSet to add a characterId to the list for the given worldId
                var update = Builders<BsonDocument>.Update.AddToSet($"worldCharacters.{worldId}", characterId);

                var result = await m_userCollection.UpdateOneAsync(filter, update);

                // Check if any document was modified
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                // Handle exception or log it as needed
                Console.WriteLine($"Error adding characterId: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> RemoveCharacterIdAsync(string uId, int worldId, string characterId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);

                // Use $pull to remove a characterId from the list for the given worldId
                var update = Builders<BsonDocument>.Update.Pull($"worldCharacters.{worldId}", characterId);

                var result = await m_userCollection.UpdateOneAsync(filter, update);

                // Check if any document was modified
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                // Handle exception or log it as needed
                Console.WriteLine($"Error removing characterId: {ex.Message}");
                return false;
            }
        }
        public async Task<List<string>> GetCharacterIdsAsync(string uId, int worldId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var projection = Builders<BsonDocument>.Projection.Include($"worldCharacters.{worldId}");
                var document = await m_userCollection.Find(filter).Project(projection).FirstOrDefaultAsync();

                if (document != null && document.Contains("worldCharacters"))
                {
                    var worldCharacters = document["worldCharacters"] as BsonDocument;
                    if (worldCharacters != null && worldCharacters.Contains(worldId.ToString()))
                    {
                        var characterIdsBsonArray = worldCharacters[worldId.ToString()].AsBsonArray;

                        // Convert the BsonArray of character IDs to List<int>
                        return characterIdsBsonArray.Select(id => id.AsString).ToList();
                    }
                }

                return new List<string>(); // Return an empty list if no characters found
            }
            catch (Exception ex)
            {
                // Handle exception or log it as needed
                Console.WriteLine($"Error retrieving character IDs: {ex.Message}");
                return new List<string>(); // Return an empty list on error
            }
        }

    }
}


