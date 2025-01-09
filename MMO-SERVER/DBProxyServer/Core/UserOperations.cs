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
                return dBUserNode;
            }

            return null;
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
        public async Task AddCharacterIdAsync(string uId, string characterId)
        {
            try
            {
                var objectId = new ObjectId(uId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
                var update = Builders<BsonDocument>.Update.AddToSet("characterIds", characterId);

                var result = await m_userCollection.UpdateOneAsync(filter, update);

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
        public async Task<bool> DeleteUserByUidAsync(string uId)
        {
            var objectId = new ObjectId(uId);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
            try
            {
                // 执行删除操作
                var deleteResult = await m_userCollection.DeleteOneAsync(filter);
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


