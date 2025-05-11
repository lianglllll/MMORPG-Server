using Common.Summer.Net;
using Google.Protobuf;
using HS.Protobuf.Combat.Skill;
using HS.Protobuf.Scene;
using HS.Protobuf.SceneEntity;
using SceneServer.Core.Combat.Skills;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene;
using SceneServer.Core.Scene.Component;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;

namespace SceneServer.Core.Combat
{
    /// <summary>
    /// 战斗管理器
    /// </summary>
    public class FightManager
    {
        // TODO 当前场景下的投射物列表
        public List<Missile> missiles = new();        

        // 待处理的技能施法队列：收集来自各个客户端的施法请求,线性处理，避免多线程并发问题。
        public ConcurrentQueue<CastInfo> castReqQueue = new();

        // 等待广播队列存在的意义：收集某帧的全部数据一起发送，减少数据包的发送频率

        // 等待广播：技能施法队列：通知各个客户端谁谁谁要施法技能
        public ConcurrentQueue<CastInfo> spellSkillQueue = new();
        // 等待广播：伤害队列：告诉客户端谁谁谁收到伤害了，让其播放一些动画/特效或者ui之类的。这里不做属性更新
        public ConcurrentQueue<Damage> damageQueue = new();
        // 等待广播：人物属性更新的队列
        public ConcurrentQueue<ActorPropertyUpdate> propertyUpdateQueue = new();

        public void Init()
        {
            ProtoHelper.Instance.Register<ActorPropertyUpdateRsponse>((int)SceneProtocl.ActorPropertyUpdateResp);
            ProtoHelper.Instance.Register<DamageResponse>((int)SkillProtocol.DamageResp);
        }
        public void UnInit()
        {
        }
        public void Update(float deltaTime)
        {
            // 处理施法请求
            HandleSkillCast();

            // 处理飞行物的逻辑更新
            HandleMissiles(deltaTime);

            // ======缓存aoi

            // 广播施法请求
            BroadcastSpellInfo();

            // 广播伤害信息
            BroadcastDamage();
            
            // 广播actor属性更新信息
            BroadcastProperties();
            // ======
        }
        private void HandleSkillCast()
        {
            while (castReqQueue.TryDequeue(out var cast))
            {
                // 1.判断施法者是否存在
                var caster = SceneEntityManager.Instance.GetSceneEntityById(cast.CasterId) as SceneActor;
                if (caster == null)
                {
                    Log.Error("RunCast: Caster is null {0}", cast.CasterId);
                    continue;
                }

                // 2.施法技能
                caster.SkillSpell.RunCast(cast);
            }
        }
        private void HandleMissiles(float deltaTime)
        {
            for (int i = 0; i < missiles.Count; i++)
            {
                missiles[i].OnUpdate(deltaTime);
            }
        }
        private void BroadcastSpellInfo()
        {
            if (spellSkillQueue.Count == 0)
            {
                goto End;
            }

            Dictionary<int, SpellCastResponse> dict = new();
            int curSceneId = SceneManager.Instance.SceneId;
            while (spellSkillQueue.TryDequeue(out var castInfo))
            {
                var entityAoiView = SceneManager.Instance.AoiZone.GetAoiEntityById(castInfo.CasterId);
                var relativeEntityIds = entityAoiView.ViewEntity;
                relativeEntityIds.Add(castInfo.CasterId);
                foreach (var entityId in relativeEntityIds)
                {
                    if (!dict.TryGetValue((int)entityId, out var resp))
                    {
                        resp = new SpellCastResponse();
                        resp.SceneId = curSceneId;
                        dict.Add((int)entityId, resp);
                    }
                    resp.List.Add(castInfo);
                }
            }

            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(dict.Keys.ToList());
            foreach (var cc in units.OfType<SceneCharacter>())
            {
                var resp = dict[cc.EntityId];
                resp.SessionId = cc.SessionId;
                cc.Send(resp);
            }

        End:
            return;
        }
        private void BroadcastDamage()
        {
            if(damageQueue.Count == 0) return;

            Dictionary<int, DamageResponse> dict = new();
            while (damageQueue.TryDequeue(out var damage))
            {
                var entityAoiView = SceneManager.Instance.AoiZone.GetAoiEntityById(damage.TargetId);
                var relativeEntityIds = entityAoiView.ViewEntity;
                relativeEntityIds.Add(damage.TargetId);
                foreach (var entityId in relativeEntityIds)
                {
                    if (!dict.TryGetValue((int)entityId, out var resp))
                    {
                        resp = new DamageResponse();
                        dict.Add((int)entityId, resp);
                    }
                    resp.Damages.Add(damage);
                }
            }

            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(dict.Keys.ToList());
            foreach (var cc in units.OfType<SceneCharacter>())
            {
                var resp = dict[cc.EntityId];
                resp.SessionId = cc.SessionId;
                cc.Send(resp);
            }
        }
        private void BroadcastProperties()
        {
            if(propertyUpdateQueue.Count == 0)
            {
                return;
            }

            Dictionary<int, ActorPropertyUpdateRsponse> dict = new();
            while (propertyUpdateQueue.TryDequeue(out var propertyUpdate))
            {
                var entityAoiView = SceneManager.Instance.AoiZone.GetAoiEntityById(propertyUpdate.EntityId);
                var relativeEntityIds = entityAoiView.ViewEntity;
                relativeEntityIds.Add(propertyUpdate.EntityId);
                foreach (var entityId in relativeEntityIds)
                {
                    if(!dict.TryGetValue((int)entityId, out var resp))
                    {
                        resp = new ActorPropertyUpdateRsponse();
                        dict.Add((int)entityId, resp);
                    }
                    resp.Propertys.Add(propertyUpdate);
                }
            }

            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(dict.Keys.ToList());
            foreach (var cc in units.OfType<SceneCharacter>())
            {
                var resp = dict[cc.EntityId];
                resp.SessionId = cc.SessionId;
                cc.Send(resp);
            }
        }
    }
}
