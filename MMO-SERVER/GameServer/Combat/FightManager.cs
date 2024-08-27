using GameServer.core;
using GameServer.Manager;
using GameServer.Model;
using Proto;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat
{
    /// <summary>
    /// 战斗管理器
    /// 这里接收到的事件都会等到下一帧开始运行的时候再处理
    /// </summary>
    public class FightManager
    {
        private Space space;

        //等待处理的技能施法队列：收集来自各个客户端的施法请求
        //这个队列维持了actor属性的同步，比如说hp的计算是单线程的。
        public ConcurrentQueue<CastInfo> castInfoQueue = new ConcurrentQueue<CastInfo>();

        //当前场景下的投射物列表
        public List<Missile> missiles = new List<Missile>();

        //等待广播的技能施法队列：通知各个客户端谁谁谁要施法技能
        public ConcurrentQueue<CastInfo> spellQueue = new ConcurrentQueue<CastInfo>();
        //响应包
        private SpellCastResponse spellCastResponse = new SpellCastResponse();

        //等待广播的伤害队列：告诉客户端谁谁谁收到伤害了，让其播放一些动画/特效或者ui之类的。这里不做属性更新
        public ConcurrentQueue<Damage> damageQueue = new ConcurrentQueue<Damage>();
        private DamageResponse damageResponse = new DamageResponse();
        
        //等待广播：人物属性更新的队列
        public ConcurrentQueue<PropertyUpdate> propertyUpdateQueue = new ConcurrentQueue<PropertyUpdate>();
        private PropertyUpdateRsponse propertyUpdateRsponse = new PropertyUpdateRsponse();
        
        public void Init(Space space)
        {
            this.space = space;
        }

        public void OnUpdate(float deltaTime)
        {
            //处理施法请求
            while(castInfoQueue.TryDequeue(out var cast))
            {
                RunCast(cast);
            }

            //处理飞行物的逻辑更新
            for (int i = 0; i < missiles.Count; i++)
            {
                missiles[i].OnUpdate(deltaTime);
            }

            //广播施法请求
            BroadcastSpellInfo();
            //广播伤害信息
            BroadcastDamage();
            //广播actor属性更新信息
            BroadcastProperties();
        }

        /// <summary>
        /// 处理施法请求
        /// </summary>
        /// <param name="cast"></param>
        private void RunCast(CastInfo cast)
        {
            //1.判断施法者是否存在
            var caster = EntityManager.Instance.GetEntityById(cast.CasterId) as Actor;
            if (caster == null)
            {
                Log.Error("RunCast: Caster is null {0}", cast.CasterId);
                return;
            }
            //2.施法技能
            caster.spell.RunCast(cast);
        }

        /// <summary>
        /// 向场景中的全部client广播施法信息
        /// </summary>
        private void BroadcastSpellInfo()
        {
            while(spellQueue.TryDequeue(out var item))
            {
                spellCastResponse.List.Add(item);
            }

            if(spellCastResponse.List.Count() > 0)
            {
                //找出所有受影响的玩家们,这里使用set是保证唯一性
                //谁需要看到这个施法的过程，当然是施法者九宫格范围内的玩家。
                var hashSet = new HashSet<Character>();
                foreach (var item in spellCastResponse.List)
                {
                    //当事人
                    var entity = GameTools.GetActorByEntityId(item.CasterId);
                    if (entity == null) continue;
                    var li = space?.aoiZone.FindViewEntity(entity.EntityId, true);
                    if (li != null)
                    {
                        foreach (Character cc in li.OfType<Character>())
                        {
                            hashSet.Add(cc);
                        }
                    }
                }

                //广播
                //space.Broadcast(spellCastResponse);
                foreach (var cc in hashSet)
                {
                    cc.session.Send(spellCastResponse);
                }

                spellCastResponse.List.Clear();
            }

        }

        /// <summary>
        /// 向场景中的全部client广播伤害信息
        /// </summary>
        private void BroadcastDamage()
        {
            while(damageQueue.TryDequeue(out var item))
            {
                damageResponse.List.Add(item);
            }
            if(damageResponse.List.Count > 0)
            {
                //找出所有受影响的玩家们,这里使用set是保证唯一性
                //谁需要看到这个伤害信息，当然是受伤害目标九宫格范围内的玩家。
                var hashSet = new HashSet<Character>();
                foreach (var item in damageResponse.List)
                {
                    //当事人,你需要让这个给
                    var entity = GameTools.GetActorByEntityId(item.TargetId);
                    if (entity == null) continue;
                    var li = space?.aoiZone.FindViewEntity(entity.EntityId, true);
                    if (li != null)
                    {
                        foreach (Character cc in li.OfType<Character>())
                        {
                            hashSet.Add(cc);
                        }
                    }
                }

                //广播
                //space.Broadcast(damageResponse);
                foreach (var cc in hashSet)
                {
                    cc.session.Send(damageResponse);
                }

                damageResponse.List.Clear();
            }

        }

        /// <summary>
        /// 向场景中的全部client广播actor的属性更新信息
        /// </summary>
        private void BroadcastProperties()
        {
            while (propertyUpdateQueue.TryDequeue(out var item))
            {
                propertyUpdateRsponse.List.Add(item);
            }
            if (propertyUpdateRsponse.List.Count > 0)
            {
                //找出所有受影响的玩家们,这里使用set是保证唯一性
                //谁需要看到这个属性变换的过程，当然是属性变化者view范围内的玩家。
                var hashSet = new HashSet<Character>();
                foreach(var item in propertyUpdateRsponse.List)
                {
                    //当事人
                    var entity = GameTools.GetActorByEntityId(item.EntityId);
                    if (entity == null) continue;
                    var li = space?.aoiZone.FindViewEntity(entity.EntityId, true);
                    if (li != null)
                    {
                        foreach(Character cc in li.OfType<Character>())
                        {
                            hashSet.Add(cc);
                        }
                    }
                }

                //广播
                foreach (var cc in hashSet)
                {
                    cc.session.Send(propertyUpdateRsponse);
                }

                propertyUpdateRsponse.List.Clear();
            }

        }

    }
}
