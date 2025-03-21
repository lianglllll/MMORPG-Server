using System.Collections.Concurrent;
using Common.Summer.Tools;
using Common.Summer.Core;
using SceneServer.Core.Model;

namespace SceneServer.Core.Scene.Component
{
    public class SceneEntityManager : Singleton<SceneEntityManager>
    {
        private IdGenerator _idGenerator = new IdGenerator();
        private ConcurrentDictionary<int, SceneEntity> allEntitiesDict = new(); // <entityId, SceneEntity>

        public override void Init()
        {
            Scheduler.Instance.Update(Update);
        }

        public void Update()
        {
            foreach (var entity in allEntitiesDict)
            {
                entity.Value.Update(MyTime.deltaTime);
            }
        }
        public void AddSceneEntity(SceneEntity entity)
        {
            lock (this)
            {
                //给角色分配一个独一无二的id
                entity.EntityId = _idGenerator.GetId();
                allEntitiesDict[entity.EntityId] = entity;
            }
        }
        public void RemoveSceneEntity(int entityId)
        {
            if (!allEntitiesDict.ContainsKey(entityId)) return;
            allEntitiesDict.TryRemove(entityId, out var entity);
            if (entity != null)
            {
                _idGenerator.ReturnId(entityId);
            }
        }
        public SceneEntity GetSceneEntityById(int entityId)
        {
            return allEntitiesDict.GetValueOrDefault(entityId);
        }
        public List<SceneEntity> GetSceneEntitiesByIds(IEnumerable<long> ids)
        {
            List<SceneEntity> res = new();
            foreach (var id in ids)
            {
                if (allEntitiesDict.TryGetValue((int)id, out var entity))
                {
                    res.Add(entity);
                }
            }
            return res;
        }
        public bool SceneEntityIsExists(int entityId)
        {
            return allEntitiesDict.ContainsKey(entityId);
        }
    }
}

