using Common.Summer.Tools;
using MongoDB.Driver;
using Serilog;

namespace DBProxyServer.Core
{
    public class MongoDBConnection:Singleton<MongoDBConnection>
    {
        private IMongoDatabase? m_database;

        public void  Init(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            m_database = client.GetDatabase(databaseName);
            UserOperations.Instance.Init(this);
            CharacterOperations.Instance.Init(this);
            WorldOperations.Instance.Init(this);
            Log.Information("Successfully connect to MongoDB");
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return m_database.GetCollection<T>(collectionName);
        }
    }
}
