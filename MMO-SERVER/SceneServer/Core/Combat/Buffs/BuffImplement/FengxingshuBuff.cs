using SceneServer.Core.Combat.Attrubute;
using SceneServer.Utils;

namespace GameServer.Buffs.BuffImplement
{
    public class FengxingshuBuff : BuffBase
    {
        private AttrubuteData attrubuteData;

        public override BuffDefine GetBuffDefine()
        {
            return StaticDataManager.Instance.buffDefineDict.GetValueOrDefault(4);
        }

        public override void Update(float delta)
        {

        }

        public override void OnGet()
        {
            attrubuteData = new()
            {
                Speed = 7000,
                DodgeRate = 10,
            };
            // Owner.Attr.buff.Merge(attrubuteData);
            // Owner.Attr.Reload();
        }

        public override void OnLost()
        {
            // Owner.Attr.buff.Sub(attrubuteData);
            // Owner.Attr.Reload();
        }

        protected override void OnLevelChange(int change)
        {

        }
    }
}
