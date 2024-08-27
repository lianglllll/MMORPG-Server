using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core
{
    public class GameTools
    {


        public static Actor GetActorByEntityId(int entityId)
        {
            return EntityManager.Instance.GetEntityById(entityId) as Actor;
        }


        public static Entity GetEntityByEntityId(int entityId)
        {
            return EntityManager.Instance.GetEntityById(entityId);
        }

        public static IEnumerable<Actor> RangeUnit(Entity entity,float range,bool includeSelf = true)
        {
            if(entity?.currentSpace == null) {
                return new HashSet<Actor>();
            }

            //转换为客户端坐标
            Vector3 pos = (Vector3)entity.Position * 0.001f;

            //通过aoi查找矩形范围内的角色
            var space = entity.currentSpace;
            var hanle = space.aoiZone.Refresh(entity.EntityId,new System.Numerics.Vector2(range, range));
            if(hanle.ViewEntity == null) {
                return new List<Actor>();
            }
            var all = EntityManager.Instance.GetEntitiesByIds(hanle.ViewEntity);
            if (includeSelf)
            {
                all.Add(entity);
            }

            //筛选圆形范围
            var res = all.Where((e) => { 
                Vector3 targetPos = e.Position;
                var dis = Vector3.Distance(pos, targetPos*0.001f);
                return !float.IsNaN(dis) && dis <= range; 
            }).OfType<Actor>();

            return res;
        }


    }
}
