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


        /// <summary>
        /// 根据entityId获取一个actor
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static Actor GetUnit(int entityId)
        {
            return EntityManager.Instance.GetEntity(entityId) as Actor;
        }

        /// <summary>
        /// 返回指定范围内的actor
        /// </summary>
        /// <param name="spaceId"></param>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static List<Actor> RangActor(int spaceId, Vector3 position,int range)
        {
            Predicate<Actor> match = (e) =>
            {
                var dis = Vector3.Distance(position, e.Position);
                return !float.IsNaN(dis) && dis <= range;
            };
            return EntityManager.Instance.GetEntityList(spaceId, match);
            }

        /// <summary>
        /// 返回指定范围内的itementity
        /// </summary>
        /// <param name="spaceId"></param>
        /// <param name="pos"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static List<ItemEntity> RangeItem(int spaceId,Vector3 pos,int range)
        {
            Predicate<ItemEntity> match = (e) =>
            {
                return Vector3Int.Distance(pos, e.Position) <= range;
            };
            return EntityManager.Instance.GetEntityList(spaceId, match);
        }

        public static Entity GetEntity(int entityId)
        {
            return EntityManager.Instance.GetEntity(entityId);
        }

    }
}
