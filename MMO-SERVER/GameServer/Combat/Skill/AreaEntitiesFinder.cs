using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat
{
    public class AreaEntitiesFinder
    {
        //某点为中心的圆形区域
        public static List<Entity> GetEntitiesInCircleAroundPoint()
        {
            //插入一个假实体进入aoi空间
            //拿到信息后再退出来。
            return null;
        }

        //某entity为中心的圆形区域
        public static IEnumerable<Actor> GetEntitiesInCircleAroundEntity(Entity entity, float range, bool includeSelf = false)
        {
            if (entity?.currentSpace == null)
            {
                return new HashSet<Actor>();
            }

            //转换为客户端坐标
            Vector3 pos = (Vector3)entity.Position * 0.001f;

            //通过aoi查找矩形范围内的角色
            var space = entity.currentSpace;
            var hanle = space.aoiZone.Refresh(entity.EntityId, new System.Numerics.Vector2(range, range));
            if (hanle.ViewEntity == null)
            {
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
                var dis = Vector3.Distance(pos, targetPos * 0.001f);
                return !float.IsNaN(dis) && dis <= range;
            }).OfType<Actor>();

            return res;
        }

        //某entity为中心的扇形区域
        public static List<Actor> GetEntitiesInSectorAroundEntity(Actor originActor, float detectionAngle, float detectionRadius)
        {
            List<Actor> entityList = originActor.currentSpace.aoiZone.FindViewEntity(originActor.EntityId, false).OfType<Actor>().ToList<Actor>();
            List<Actor> result = new List<Actor>();
            foreach (var target in entityList)
            {
                if (AreaEntitiesFinder.CheckForLegalSectorArea(originActor, target, detectionAngle, detectionRadius))
                {
                    result.Add(target);
                }
            }
            return result;
        }

        //某entity为中心的矩形区域
        public static List<Actor> GetEntitiesInRectangleAroundEntity(Actor originActor,float length, float width)
        {
            List<Actor> entityList = originActor.currentSpace.aoiZone.FindViewEntity(originActor.EntityId, false).OfType<Actor>().ToList<Actor>();
            List<Actor> result = new List<Actor>();
            foreach (var target in entityList)
            {
                if (AreaEntitiesFinder.CheckForLegalRectangularArea(originActor, target, length, width))
                {
                    result.Add(target);
                }
            }
            return result;
        }

        /// <summary>
        /// 检查当前目标是否在合法的扇形区域中
        /// </summary>
        public static bool CheckForLegalSectorArea(Actor originActor,Actor targetActor, float detectionAngle, float detectionRadius)
        {
            Vector3Int pos = originActor.Position;
            Vector3Int dir = originActor.Direction;

            // 将欧拉角转换为弧度
            float yaw = dir.y * Mathf.Deg2Rad * 0.001f;

            // 计算当前技能拥有者朝向的单位向量
            Vector3 forwardVector = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));

            // 计算角色到敌人的向量
            Vector3 toEnemy = targetActor.Position - pos;

            // 计算角度差
            float angle = Vector3.Angle(forwardVector, toEnemy);

            // 如果敌人在扇形区域内并且在检测半径范围内
            if (angle <= detectionAngle / 2 && toEnemy.magnitude <= detectionRadius)
            {
                // 在这里可以执行相应的逻辑，比如标记敌人等
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查当前目标是否在合法的矩形区域内
        /// </summary>
        public static bool CheckForLegalRectangularArea(Actor originActor, Actor targetActor, float length, float width)
        {

            Vector3Int pos = originActor.Position;
            Vector3Int dir = originActor.Direction;

            // 计算目标相对于所有者的位置向量
            Vector3 directionToTarget = targetActor.Position - pos;

            // 将欧拉角转换为弧度
            float yaw = dir.y * Mathf.Deg2Rad * 0.001f;

            // 计算角色朝向的单位向量
            Vector3 forwardVector = new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw));

            // 计算这个向量在所有者面向方向上的投影长度，即目标在所有者前方多远
            float forwardDistance = Vector3.Dot(directionToTarget, forwardVector);

            // 如果目标在所有者背后，或者超出了指定的长度，则不在区域内
            if (forwardDistance < 0 || forwardDistance > length)
            {
                return false;
            }


            // 计算右向单位向量
            Vector3 rightVector = Vector3.Cross(Vector3.up, forwardVector).normalized;

            // 计算目标在所有者右侧的距离，以判断宽度
            float rightDistance = Vector3.Dot(directionToTarget, rightVector);

            // 如果目标相对于中心的距离超出了宽度的一半，则不在区域内
            if (Mathf.Abs(rightDistance) > width / 2)
            {
                return false;
            }

            // 如果以上条件都不满足，则目标在矩形区域内
            return true;
        }

    }
}
