using SceneServer.Core.Model.Actor;
using SceneServer.Utils;
using System.Collections.Generic;

namespace SceneServer.Core.Combat.Buffs.BuffImplement
{
    // 无敌buff,  有这个buff的actor无法被攻击(也就是无法受到伤害)
    [BuffAttribute(5)]
    public class InvincibleBuff : BuffBase
    {
        public override BuffDefine GetBuffDefine()
        {
            return StaticDataManager.Instance.buffDefineDict.GetValueOrDefault(5);
        }
        public override void OnGet()
        {
            Owner.SetActorReciveDamageMode(false);
        }
        public override void OnLost()
        {
            Owner.SetActorReciveDamageMode(true);
        }
    }
}
