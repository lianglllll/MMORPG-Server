using Common.Summer.Tools;
using Google.Protobuf;
using HS.Protobuf.DBProxy.DBCharacter;
using HS.Protobuf.DBProxy.DBTask;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBProxyServer.Core
{
    public class InventoryOperations : Singleton<InventoryOperations>
    {
        private IMongoCollection<BsonDocument>? m_inventoryCollection;
        public void Init(MongoDBConnection dbConnection)
        {
            m_inventoryCollection = dbConnection.GetCollection<BsonDocument>("Inventory");
        }
        public async Task<DBInventorys> GetDBInventorysByCid(string cId)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("cId", cId);
                var projection = Builders<BsonDocument>.Projection
                    .Include("backpackData")
                    .Include("equipsData");

                var document = await m_inventoryCollection
                    .Find(filter)
                    .Project(projection)
                    .FirstOrDefaultAsync();

                var result = new DBInventorys();

                if (document != null)
                {
                    // 处理背包数据（Protobuf bytes类型适配）
                    if (document.TryGetValue("backpackData", out var backpackValue)
                        && backpackValue.IsBsonBinaryData)
                    {
                        var binData = backpackValue.AsBsonBinaryData;
                        // 关键转换：byte[] → ByteString
                        result.BackpackData = ByteString.CopyFrom(binData.Bytes);
                    }

                    // 处理装备数据
                    if (document.TryGetValue("equipsData", out var equipsValue)
                        && equipsValue.IsBsonBinaryData)
                    {
                        var binData = equipsValue.AsBsonBinaryData;
                        result.EquipsData = ByteString.CopyFrom(binData.Bytes);
                    }
                }

                return result;
            }
            catch (MongoException ex)
            {
                Log.Error($"数据库查询失败 cId:{cId}. {ex}");
                return new DBInventorys();
            }
        }
        public async Task<bool> SaveDBInventorys(string cId, DBInventorys inventorys)
        {
            const int maxRetry = 2;
            var attempt = 0;

            while (attempt <= maxRetry)
            {
                try
                {
                    // 参数校验
                    if (string.IsNullOrEmpty(cId))
                        throw new ArgumentException("Invalid character ID", nameof(cId));

                    if (inventorys == null)
                        throw new ArgumentNullException(nameof(inventorys));

                    // 动态构建更新操作
                    var updateBuilder = Builders<BsonDocument>.Update;
                    var updateDef = updateBuilder.SetOnInsert("cId", cId)
                                                .Set("lastUpdate", DateTime.UtcNow);

                    // 仅在数据非空时更新对应字段
                    if (inventorys.BackpackData != null && !inventorys.BackpackData.IsEmpty)
                    {
                        var backpackBytes = inventorys.BackpackData.ToByteArray();
                        updateDef = updateDef.Set("backpackData",
                            new BsonBinaryData(backpackBytes, BsonBinarySubType.Binary));
                    }

                    if (inventorys.EquipsData != null && !inventorys.EquipsData.IsEmpty)
                    {
                        var equipsBytes = inventorys.EquipsData.ToByteArray();
                        updateDef = updateDef.Set("equipsData",
                            new BsonBinaryData(equipsBytes, BsonBinarySubType.Binary));
                    }

                    // 配置更新选项
                    var options = new UpdateOptions
                    {
                        IsUpsert = true,
                        BypassDocumentValidation = false
                    };

                    // 执行原子更新
                    var result = await m_inventoryCollection.UpdateOneAsync(
                        filter: Builders<BsonDocument>.Filter.Eq("cId", cId),
                        update: updateDef,
                        options: options
                    );

                    // 检查更新结果
                    if (result.IsAcknowledged && (result.ModifiedCount > 0 || result.UpsertedId != null))
                    {
                        return true;
                    }

                    // 重试逻辑
                    if (attempt++ >= maxRetry) break;
                    await Task.Delay(50 * attempt);
                }
                catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    Log.Warning($"并发冲突 cId:{cId}, 重试次数:{attempt}");
                    if (attempt++ >= maxRetry) throw;
                }
                catch (Exception ex)
                {
                    Log.Error($"保存背包数据失败 cId:{cId}. {ex}");
                    return false;
                }
            }
            return false;
        }
        public async Task<bool> RemoveDBInventorysByCid(string cId)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("cId", cId);
            var deleteResult = await m_inventoryCollection.DeleteOneAsync(filter);
            // 返回是否成功删除一条文档
            return deleteResult.DeletedCount > 0;
        }
    }
}
