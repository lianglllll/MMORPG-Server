using Common.Summer;
using GameServer.Combat;
using GameServer.Core;
using GameServer.Model;
using Summer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
    /// <summary>
    /// Entity管理器,(character,怪物，npc,陷阱)
    /// 记录下这些之后，就能很快查找你附近有些什么人，什么怪物
    /// </summary>
    public class EntityManager : Singleton<EntityManager>
    {
        private  IdGenerator _idGenerator = new IdGenerator();

        //记录全部entity对象<entityId,entity>
        private ConcurrentDictionary<int, Entity> allEntitiesDict = new ConcurrentDictionary<int, Entity>();
        //记录单个场景中的entity列表，<spaceId,entityList>
        private ConcurrentDictionary<int, List<Entity>> spaceEntitiesDict = new ConcurrentDictionary<int, List<Entity>>();
        
        public void Update()
        {
            foreach (var entity in allEntitiesDict)
            {
                entity.Value.Update();
            }
        }

        public void AddEntity(int spaceId, Entity entity)
        {
            lock (this)
            {
                //给角色分配一个独一无二的id
                //给entity里面的netObj赋值了
                entity.EntityId = _idGenerator.GetId();
                allEntitiesDict[entity.EntityId] = entity;

                if (!spaceEntitiesDict.ContainsKey(spaceId))
                {
                    spaceEntitiesDict[spaceId] = new List<Entity>();
                }

                if (spaceEntitiesDict.TryGetValue(spaceId, out var list))
                {
                    lock (list)
                    {
                        list.Add(entity);
                    }
                }
            }
        }

        public void RemoveEntity(int spaceId, int entityId)
        {
            if (!allEntitiesDict.ContainsKey(entityId)) return;
            allEntitiesDict.TryRemove(entityId, out var entity);
            if(entity != null)
            {
                _idGenerator.ReturnId(entityId);
            }
            if (spaceEntitiesDict.TryGetValue(spaceId, out var list))
            {
                lock (list)
                {
                    list.Remove(entity);
                }
            }

        }

        public Entity GetEntityById(int entityId)
        {
            return allEntitiesDict.GetValueOrDefault(entityId);
        }

        public List<T> GetEntitiesAroundPoint<T>(int spaceId, Vector3Int center, float range) where T : Entity
        {
            if (!spaceEntitiesDict.TryGetValue(spaceId, out var list)) return null;
            return list?
                .OfType<T>()                            //根据类型赛选
                .Where(entity => {                      //根据条件赛选

                    return Vector3Int.Distance(center, entity.Position) <= range;
                })  
                .ToList();
        }
        public List<Entity> GetEntitiesByIds(IEnumerable<long> ids)
        {
            List<Entity> res = new List<Entity>(); 
            foreach (var id in ids)
            {
                if(allEntitiesDict.TryGetValue((int)id,out var entity))
                {
                    res.Add(entity);
                }
            }
            return res;
        }

        public bool EntityExists(int entityId)
        {
            return allEntitiesDict.ContainsKey(entityId);
        }

        public void EntityChangeSpace(Entity entity,int oldSpaceId,int newSpaceId)
        {
            if (oldSpaceId == newSpaceId) return;
            if (spaceEntitiesDict.TryGetValue(oldSpaceId, out var list1))
            {
                lock (list1)
                {
                    list1.Remove(entity);
                }
            }
            if (spaceEntitiesDict.TryGetValue(oldSpaceId, out var list2))
            {
                lock (list2)
                {
                    list2.Add(entity);
                }
            }

        }
    }
}

