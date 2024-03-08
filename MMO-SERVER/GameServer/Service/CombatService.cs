using GameServer.core;
using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using Proto;
using Serilog;
using Summer;
using Summer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Service
{
    public class CombatService : Singleton<CombatService>
    {
        /// <summary>
        /// 开启服务
        /// </summary>
        public void Start()
        {
            MessageRouter.Instance.Subscribe<SpellCastRequest>(_SpellCastRequest);
            MessageRouter.Instance.Subscribe<ReviveRequest>(_ReviveRequest);
            MessageRouter.Instance.Subscribe<SpaceDeliverRequest>(_SpaceDeliverRequest);
        }

        /// <summary>
        /// 复活请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _ReviveRequest(Connection conn, ReviveRequest message)
        {
            var actor = EntityManager.Instance.GetEntity(message.EntityId);
            if(actor != null && actor is Character chr&& chr.IsDeath&& chr.session.Conn == conn)
            {
                //设置当前角色的位置
                //找到场景中最近的复活点
                chr.Position = chr.currentSpace.SearchNearestRevivalPoint(chr);
                chr.Revive();


                //这里应该发一个响应回去更好吧？
                //给客户端广播发送更新位置的数据
                SpaceEntitySyncResponse resp = new SpaceEntitySyncResponse();
                NEntitySync nEntitySync = new NEntitySync();
                nEntitySync.Entity = conn.Get<Session>().character.EntityData;
                nEntitySync.State = EntityState.NoneState;//客户端会处理复活逻辑，状态就无需关心了
                resp.EntitySync = nEntitySync;
                resp.EntitySync.Force = true;
                conn.Send(resp);
            }
        }

        /// <summary>
        /// 施法技能请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _SpellCastRequest(Connection conn, SpellCastRequest message)
        {
            //Log.Information("技能施法请求：{0}", message);
            //判断技能施法是否成功
            Session session  = conn.Get<Session>();
            Character chr = session.character;
            if(chr.EntityId != message.Info.CasterId)
            {
                Log.Error("施法者ID错误");
                return;
            }
            //成功，则将其放入当前场景的战斗管理器的缓冲队列中
            chr.currentSpace.fightManager.castInfoQueue.Enqueue(message.Info);
        }

        /// <summary>
        /// 传送请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void _SpaceDeliverRequest(Connection conn, SpaceDeliverRequest message)
        {
            var chr = conn.Get<Session>().character;
            if (chr == null) return;

            //todo 这应该在表中取静态数据复活点的
            var sp = SpaceManager.Instance.GetSpaceById(message.SpaceId);
            if(message.SpaceId == 0)
            {
                var def = DataManager.Instance.revivalPointDefindeDict[0];
                chr.TransmitSpace(sp, new Core.Vector3Int(def.X,def.Y,def.Z));
            }
            else if(message.SpaceId == 1)
            {
                var def = DataManager.Instance.revivalPointDefindeDict[1000];
                chr.TransmitSpace(sp, new Core.Vector3Int(def.X, def.Y, def.Z));
            }

        }



    }
}
