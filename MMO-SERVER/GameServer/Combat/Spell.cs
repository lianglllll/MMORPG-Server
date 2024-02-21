using GameServer.Core;
using GameServer.Manager;
using GameServer.Model;
using Proto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat
{

    /// <summary>
    /// 技能施法器
    /// 每个actor都有自己的技能施法器
    /// </summary>
    public class Spell
    {
        /// <summary>
        /// 技能施法器的归属者
        /// </summary>
        public Actor Owner { get; private set; }               

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Owner"></param>
        public Spell(Actor Owner)
        {
            this.Owner = Owner;
        }

        /// <summary>
        /// 同一施法，处理各种类型的施法请求
        /// </summary>
        /// <param name="castInfo"></param>
        public void RunCast(CastInfo castInfo)
        {
            var skill = Owner.skillManager.GetSkill(castInfo.SkillId);

            if (skill.IsNoneTarget)
            {
                SpellNoTarget(castInfo.SkillId);
            }
            if (skill.IsUnitTarget)
            {
                SpellTarget(castInfo.SkillId, castInfo.TargetId);
            }
            if (skill.IsPointTarget)
            {
                SpellPosition(castInfo.SkillId, castInfo.Point);
            }

        }

        /// <summary>
        /// 释放无目标技能
        /// </summary>
        /// <param name="skill_id"></param>
        private void SpellNoTarget(int skill_id)
        {

            //判断owner是否拥有这个技能
            var skill = Owner.skillManager.GetSkill(skill_id);
            if (skill == null)
            {
                Log.Warning("Spell::SpellTarget():Owner[{0}]:Skill[{1}] not found", Owner.EntityId, skill_id);
                return;
            }

            //执行技能,目标选择自己得了
            SCObject sco = new SCEntity(Owner);
            var res = skill.CanUse(sco);
            //通知施法者,执行失败
            if (res != CastResult.Success)
            {
                Log.Warning("Cast Fail Skill {0} {1}", skill.Define.ID, res);
                OnSpellFailure(skill_id, res);
                return;
            }
            //执行技能
            skill.Use(sco);

            //广播，可能本帧有很多人施法节能，那就收集本帧的info，等到下一帧再发出去
            CastInfo info = new CastInfo()
            {
                CasterId = Owner.EntityId,
                SkillId = skill_id
            };
            Owner.currentSpace.fightManager.spellQueue.Enqueue(info);
        }

        /// <summary>
        /// 释放单体目标技能
        /// </summary>
        /// <param name="skill_id"></param>
        /// <param name="target_id"></param>
        public void SpellTarget(int skill_id,int target_id)
        {
            //Log.Information("Spell::SpellTarget():Caster[{0}]:Skill[{1}]：Targert[{2}]", Owner.EntityId, skill_id, target_id);

            //判断owner是否拥有这个技能
            var skill = Owner.skillManager.GetSkill(skill_id);
            if(skill == null)
            {
                Log.Warning("Spell::SpellTarget():Owner[{0}]:Skill[{1}] not found", Owner.EntityId, skill_id);
                return;
            }

            //检测目标
            var target = EntityManager.Instance.GetEntity(target_id) as Actor;
            if(target == null)
            {
                Log.Warning("Spell::SpellTarget():Owner[{0}]:target[{1}] not found", Owner.EntityId, target_id);
                return;
            }

            //执行技能
            SCObject sco = new SCEntity(target);
            var res = skill.CanUse(sco);
            //通知施法者,执行失败
            if (res != CastResult.Success)
            {
                Log.Warning("Cast Fail Skill {0} {1}", skill.Define.ID, res);
                OnSpellFailure(skill_id, res);
                return;
            }
            //执行技能
            skill.Use(sco);

            //广播，可能本帧有很多人施法节能，那就收集本帧的info，等到下一帧再发出去
            CastInfo info = new CastInfo()
            {
                CasterId = Owner.EntityId,
                TargetId = target_id,
                SkillId = skill_id
            };
            Owner.currentSpace.fightManager.spellQueue.Enqueue(info);
        }

        /// <summary>
        /// 释放点目标技能
        /// </summary>
        /// <param name="skill_id"></param>
        /// <param name="position"></param>
        private void SpellPosition(int skill_id,Vector3 position)
        {
            Log.Information("Spell::SpellPosition():Caster[{0}]:Pos[{1}]", Owner.EntityId, position);
            SCObject sco = new SCPosition(position);
        }

        /// <summary>
        /// 吟唱技能
        /// </summary>
        /// <param name="skill"></param>
        public void Intonate(Skill skill)
        {

        }

        /// <summary>
        /// 通知玩家技能施法失败 
        /// </summary>
        /// <param name="skill_id"></param>
        /// <param name="reason"></param>
        public void OnSpellFailure(int skill_id,CastResult reason)
        {
            if(Owner is Character chr)
            {
                SpellFailResponse resp = new SpellFailResponse()
                {
                    CasterId = Owner.EntityId,
                    SkillId = skill_id,
                    Reason = reason
                };
                chr.session.Send(resp);
            }
        }

    }
}
