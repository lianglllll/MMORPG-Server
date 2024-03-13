using GameServer.Combat;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
