using Serilog;
using Common.Summer.Core;
using HS.Protobuf.Combat.Skill;
using SceneServer.Core.Model.Actor;
using SceneServer.Core.Scene.Component;
using SceneServer.Core.Scene;
using System.Diagnostics;

namespace SceneServer.Core.Combat.Skills
{
    /// <summary>
    /// 技能施法器
    /// 每个actor都有自己的技能施法器
    /// </summary>
    public class SkillSpell
    {
        public SceneActor Owner { get; private set; }

        public bool Init(SceneActor Owner)
        {
            this.Owner = Owner;
            return true;
        }

        /// <summary>
        /// 同一施法，处理各种类型的施法请求
        /// </summary>
        /// <param name="castInfo"></param>
        public void RunCast(CastInfo castInfo)
        {
            // actor处于技能后摇中，无法释放技能
            if (Owner.CurUseSkill != null && !Owner.CurUseSkill.IsCanSwitchSkill())
            {
                Log.Warning("Spell::RunCast():Owner[{0}]:Skill[{1}] skilling!", Owner.EntityId, castInfo.SkillId);
                goto End;
            }

            // 判断owner是否拥有这个技能
            var skill = Owner.GetSkillById(castInfo.SkillId);
            if (skill == null)
            {
                Log.Warning("Spell::RunCast():Owner[{0}]:Skill[{1}] not found!", Owner.EntityId, castInfo.SkillId);
                goto End;
            }

            Log.Information("Spell::RunCast():Owner[{0}]:Skill[{1}] success!", Owner.EntityId, castInfo.SkillId);

            if (skill.IsNoTarget)                           //释放无目标技能
            {
                SpellNoTarget(skill);
            }
            else if (skill.IsTarget)                       //释放单体目标技能
            {
                SpellTarget(skill, castInfo.TargetId);
            }
            else if (skill.IsPointTarget)                  //释放点目标技能
            {
                SpellPosition(skill, castInfo.Point);
            }
        End:
            return;
        }
        private void SpellNoTarget(Skill skill)
        {
            //执行技能,因为是无目标技能所有 选择自己 或者 不选
            SCObject sco = new SCEntity(Owner);
            var result = skill.CanUse(sco);

            //通知施法者,执行失败
            if (result != CastResult.Success)
            {
                OnSpellFailure(skill.SkillId, result);
                return;
            }

            //执行技能
            skill.Use(sco);

            //广播，可能本帧有很多人施法节能，那就收集本帧的info，等到下一帧再发出去
            CastInfo info = new CastInfo()
            {
                CasterId = Owner.EntityId,
                SkillId = skill.SkillId
            };
            SceneManager.Instance.FightManager.spellSkillQueue.Enqueue(info);
        }
        private void SpellTarget(Skill skill, int target_id)
        {
            //检测目标
            var target = SceneEntityManager.Instance.GetSceneEntityById(target_id) as SceneActor;
            if (target == null)
            {
                Log.Warning("Spell::SpellTarget():Owner[{0}]:target[{1}] not found", Owner.EntityId, target_id);
                return;
            }

            //检测是否能执行技能
            SCObject sco = new SCEntity(target);
            var res = skill.CanUse(sco);

            //通知施法者,执行失败
            if (res != CastResult.Success)
            {
                OnSpellFailure(skill.SkillId, res);
                return;
            }

            //执行技能
            skill.Use(sco);

            //广播，可能本帧有很多人施法节能，那就收集本帧的info，等到下一帧再发出去
            CastInfo info = new CastInfo()
            {
                CasterId = Owner.EntityId,
                TargetId = target_id,
                SkillId = skill.SkillId
            };
            // todo
            // Owner.currentSpace.fightManager.spellQueue.Enqueue(info);
        }
        private void SpellPosition(Skill skill, Vector3 position)
        {
            //检测是否能执行技能
            SCObject sco = new SCPosition(position);
            var res = skill.CanUse(sco);

            //通知施法者,执行失败
            if (res != CastResult.Success)
            {
                OnSpellFailure(skill.SkillId, res);
                return;
            }

            //执行技能
            skill.Use(sco);

            //广播，可能本帧有很多人施法节能，那就收集本帧的info，等到下一帧再发出去
            CastInfo info = new CastInfo()
            {
                CasterId = Owner.EntityId,
                SkillId = skill.SkillId,
                Point = position
            };
            // todo
            // Owner.currentSpace.fightManager.spellQueue.Enqueue(info);
        }


        /// <summary>
        /// 技能施法失败，通知玩家
        /// </summary>
        /// <param name="skill_id"></param>
        /// <param name="reason"></param>
        public void OnSpellFailure(int skill_id, CastResult reason)
        {
            Log.Warning("Cast Fail Skill {0} {1}", skill_id, reason);

            if (Owner is SceneCharacter chr)
            {
                SpellFailResponse resp = new SpellFailResponse()
                {
                    CasterId = Owner.EntityId,
                    SkillId = skill_id,
                    Reason = reason
                };
                // todo
                // chr.session.Send(resp);
            }
        }

    }
}
