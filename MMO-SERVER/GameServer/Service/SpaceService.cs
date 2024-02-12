using Summer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proto;
using Summer;
using GameServer.Model;
using GameServer.Manager;
using Serilog;
using GameServer.Core;
using GameServer.core;

namespace GameServer.Service
{
    public class SpaceService:Singleton<SpaceService>
    {

        /// <summary>
        /// 开启服务
        /// </summary>
        public void Start()
        {
            //初始化全部地图
            SpaceManager.Instance.init();

            //订阅信息同步请求
            MessageRouter.Instance.Subscribe<SpaceEntitySyncRequest>(_SpaceEntitySyncRequest);
           
        }

        /// <summary>
        /// 根据spaceId获取地图
        /// </summary>
        /// <param name="spaceId"></param>
        /// <returns></returns>
        public Space GetSpaceById(int spaceId)
        {
            return SpaceManager.Instance.GetSpaceById(spaceId);          
        }

        /// <summary>
        /// 角色信息同步请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void _SpaceEntitySyncRequest(Connection conn, SpaceEntitySyncRequest msg)
        {

            //获取当前连接的场景对象
            Space connSpace = conn.Get<Session>().Space;
            if (connSpace == null)
            {
                return;
            }


            //判断合理性
            NetEntity nEntity = msg.EntitySync.Entity;//请求位置信息
            Entity serverEntity = EntityManager.Instance.GetEntity(nEntity.Id);
            //将要移动的距离
            float distance = Vector3Int.Distance(nEntity.Position, serverEntity.Position);
            //使用服务端的速度
            nEntity.Speed = serverEntity.Speed;
            //计算时间差
            float timeDistance = Math.Min(serverEntity.PositionUpdateTimeDistance, 1.0f);
            //计算距离限额
            float limit = serverEntity.Speed * timeDistance * 1.5f;
            //Log.Information("距离{0}，阈值{1}，间隔{2}", distance, limit, timeDistance);
            if (float.IsNaN(distance)||distance > limit)
            {
                //将发出请求的客户端重置为原来合理的位置
                SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
                resp.EntitySync = new NEntitySync();
                resp.EntitySync.Entity = serverEntity.EntityData;
                resp.EntitySync.Force = true;
                conn.Send(resp);

                return;
            }

            //更新信息并且转发
            connSpace.UpdateEntity(msg.EntitySync);

        }

    }
}
