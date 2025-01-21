using GameServer.Combat;
using System.Collections.Generic;

namespace GameServer.Buffs.BuffImplement
{
    public class InvincibleBuff : BuffBase
    {
        private AttrubuteData attrubuteData;


        public override BuffDefine GetBuffDefine()
        {
            return DataManager.Instance.buffDefindeDict.GetValueOrDefault(5);
        }
    }
}
