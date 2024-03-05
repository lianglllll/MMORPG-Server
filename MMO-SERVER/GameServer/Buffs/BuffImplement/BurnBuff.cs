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
            base.OnGet();
        }

        public override void OnLost()
        {
            base.OnLost();
        }

        protected override void OnLevelChange(int change)
        {
            base.OnLevelChange(change);
        }

    }
}
