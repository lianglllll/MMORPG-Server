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
        private int index = 1;
        //记录全部entity对象<entityId,entity>
        private ConcurrentDictionary<int, Entity> allEntitiesDict = new ConcurrentDictionary<int, Entity>();
        //记录单个场景中的entity列表，<spaceId,entityList>
        private ConcurrentDictionary<int, List<Entity>> spaceEntitiesDict = new ConcurrentDictionary<int, List<Entity>>();
        
        //获取唯一的entityId
        public int NewEntityId
        {
            get
            {
                lock (this)
                {
                    return index++;
                }
            }
        }


        public void AddEntity(int spaceId, Entity entity)
        {
            lock (this)
            {
                //给角色分配一个独一无二的id
                entity.EntityId = NewEntityId;//给entity里面的netObj赋值了
                allEntitiesDict[entity.EntityId] = entity;

                if (!spaceEntitiesDict.ContainsKey(spaceId))
                {
                    spaceEntitiesDict[spaceId] = new List<Entity>();
                }
                GetSpaceEntitytList(spaceId, (list) => list.Add(entity));
            }

        }

        public void RemoveEntity(int spaceId, int entityId)
        {
            if (!allEntitiesDict.ContainsKey(entityId)) return;
            allEntitiesDict.TryRemove(entityId, out var item);
            GetSpaceEntitytList(spaceId, (list) => list.Remove(item));
        }

        /// <summary>
        /// 根据entityid获取单个entity
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public Entity GetEntity(int entityId)
        {
            return allEntitiesDict.GetValueOrDefault(entityId);
        }

        public void Update()
        {
            foreach (var entity in allEntitiesDict)
            {
                entity.Value.Update();
            }
        }

        /// <summary>
        /// 根据条件match查找entity对象(T),T:character||monster||...
        /// 比如说：生命值为0的，在某个范围内的，
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="spaceId"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public List<T> GetEntityList<T>(int spaceId,Predicate<T> match)where T : Entity
        {
            if (!spaceEntitiesDict.TryGetValue(spaceId, out var list)) return null;

            return list?
                .OfType<T>()                            //根据类型赛选
                .Where(entity => match.Invoke(entity))  //根据条件赛选
                .ToList();
        }

        /// <summary>
        /// 寻找目标点范围内的character
        /// </summary>
        /// <param name="sapceId"></param>
        /// <param name="center"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public List<Character> GetGetNearEntitys(int sapceId, Vector3Int center, int range) 
        {
            Predicate<Character> match = (e) =>
            {
                return Vector3Int.Distance(center, e.Position) <= range;
            };
            return GetEntityList<Character>(sapceId, match);
        }

        /// <summary>
        /// 判断某个entity是否存在
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool Exist(int entityId)
        {
            return allEntitiesDict.ContainsKey(entityId);
        }

        /// <summary>
        /// 获取某个场景内全部的entity
        /// </summary>
        /// <param name="spaceId"></param>
        /// <param name="action"></param>
        public void GetSpaceEntitytList(int spaceId,Action<List<Entity>> action)
        {
            if(spaceEntitiesDict.TryGetValue(spaceId,out var list))
            {
                lock (list)
                {
                    action.Invoke(list);
                }
            }
        }

        /// <summary>
        /// entity切换场景
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="oldSpaceId"></param>
        /// <param name="newSpaceId"></param>
        public void ChangeSpace(Entity entity,int oldSpaceId,int newSpaceId)
        {
            if (oldSpaceId == newSpaceId) return;
            GetSpaceEntitytList(oldSpaceId, (list) => list.Remove(entity));
            GetSpaceEntitytList(newSpaceId, (list) => list.Add(entity));
        }
    }
}

