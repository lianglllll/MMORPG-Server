using GameServer.Combat;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Buffs.BuffImplement
{
    public class FengxingshuBuff : BuffBase
    {
        private AttrubuteData attrubuteData;


        public override BuffDefine GetBuffDefine()
        {
            return DataManager.Instance.buffDefindeDict.GetValueOrDefault(4);
        }

        public override void OnUpdate(float delta)
        {

        }

        public override void OnGet()
        {
            attrubuteData = new()
            {
                Speed = 7000,
                DodgeRate = 10,
            };
            Owner.Attr.buff.Merge(attrubuteData);
            Owner.Attr.Reload();
        }

        public override void OnLost()
        {
            Owner.Attr.buff.Sub(attrubuteData);
            Owner.Attr.Reload();
        }

        protected override void OnLevelChange(int change)
        {

        }
    }
}
