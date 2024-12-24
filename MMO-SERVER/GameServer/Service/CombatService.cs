using GameServer.Manager;
using GameServer.Model;
using Serilog;
using GameServer.Net;
using Common.Summer.Tools;
using Common.Summer.Net;
using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;

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
        /// 复活请求
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="message"></param>
        private void _ReviveRequest(Connection conn, ReviveRequest message)
        {
            var actor = EntityManager.Instance.GetEntityById(message.EntityId);

            if (actor != null && actor is Character chr && chr.session.Conn == conn && chr.IsDeath)
            {
                chr.currentSpace.actionQueue.Enqueue(() =>
                {
                    chr.Revive();
                });
            }
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
            var sp = SpaceManager.Instance.GetSpaceById(message.SpaceId);
            DataManager.Instance.revivalPointDefindeDict.TryGetValue(message.PointId, out var pointDef);
            if (sp == null || pointDef == null) return;
            chr.TransmitTo(sp, new Vector3Int(pointDef.X, pointDef.Y, pointDef.Z));

        }
    }
}
