using SceneServer.Core.Combat.Attrubute;
using SceneServer.Utils;

namespace GameServer.Buffs.BuffImplement
{
    public class InvincibleBuff : BuffBase
    {
        private AttrubuteData attrubuteData;


        public override BuffDefine GetBuffDefine()
        {
            return StaticDataManager.Instance.buffDefineDict.GetValueOrDefault(5);
        }
    }
}
