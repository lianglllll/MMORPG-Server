using Proto;
using GameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Buffs.BuffImplement
{
    /// <summary>
    /// 燃烧buff
    /// </summary>
    public class BurnBuff : BuffBase
    {

        public BurnBuff() { }

        public override BuffDefine GetBuffDefine()
        {
            return DataManager.Instance.buffDefindeDict.GetValueOrDefault(1);
        }

        public override void OnGet()
        {
            Scheduler.Instance.AddTask(() =>
            {
                if (Owner.IsDeath) return;
                var dmg = new Damage
                {
                    AttackerId = Provider.EntityId,
                    TargetId = Owner.EntityId,
                    SkillId = 0,
                    Amount = 10,
                };
                Owner.RecvDamage(dmg);

            }, 1, (int)Def.MaxDuration);
        }

        public override void OnLost()
        {
        }

        protected override void OnLevelChange(int change)
        {
        }

    }
}
