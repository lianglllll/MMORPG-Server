using Newtonsoft.Json;


namespace SceneServer.Core.Combat.Attrubute
{
    //属性数据
    //这个属性数据可以包含在你的装备里面、buffer里面
    //这些个属性数据都是可以合并到一起，形成角色最终的属性
    public class AttrubuteData
    {
        //速度
        public int Speed;
        //最大生命值
        public float HPMax;
        //最大魔法值
        public float MPMax;
        //物理攻击
        public float AD;
        //魔法攻击
        public float AP;
        //物理防御
        public float DEF;
        //魔法防御
        public float MDEF;
        //暴击率
        public float CRI;
        //暴击伤害
        public float CRD;
        // 命中率%
        public float HitRate;
        // 闪避率%
        public float DodgeRate;
        // 生命恢复/秒
        public float HpRegen;
        // 伤害吸血%
        public float HpSteal;
        //力量
        public float STR;
        //智力
        public float INT;
        //敏捷
        public float AGI;

        //add
        //将其他属性数据融合到这个属性当中
        public void Merge(AttrubuteData attrubuteData)
        {
            Speed += attrubuteData.Speed;
            HPMax += attrubuteData.HPMax;
            MPMax += attrubuteData.MPMax;
            AD += attrubuteData.AD;
            AP += attrubuteData.AP;
            DEF += attrubuteData.DEF;
            MDEF += attrubuteData.MDEF;
            CRI += attrubuteData.CRI;
            CRD += attrubuteData.CRD;
            HitRate += attrubuteData.HitRate;
            DodgeRate += attrubuteData.DodgeRate;
            HpRegen += attrubuteData.HpRegen;
            HpSteal += attrubuteData.HpSteal;
            STR += attrubuteData.STR;
            INT += attrubuteData.INT;
            AGI += attrubuteData.AGI;
        }

        //delete
        //减少属性
        public void Sub(AttrubuteData attrubuteData)
        {
            Speed -= attrubuteData.Speed;
            HPMax -= attrubuteData.HPMax;
            MPMax -= attrubuteData.MPMax;
            AD -= attrubuteData.AD;
            AP -= attrubuteData.AP;
            DEF -= attrubuteData.DEF;
            MDEF -= attrubuteData.MDEF;
            CRI -= attrubuteData.CRI;
            CRD -= attrubuteData.CRD;
            HitRate -= attrubuteData.HitRate;
            DodgeRate -= attrubuteData.DodgeRate;
            HpRegen -= attrubuteData.HpRegen;
            HpSteal -= attrubuteData.HpSteal;
            STR -= attrubuteData.STR;
            INT -= attrubuteData.INT;
            AGI -= attrubuteData.AGI;
        }


        //属性清空
        public void Reset()
        {
            Speed = 0;
            HPMax = 0;
            MPMax = 0;
            AD = 0;
            AP = 0;
            DEF = 0;
            MDEF = 0;
            CRI = 0;
            CRD = 0;
            HitRate = 0;
            DodgeRate = 0;
            HpRegen = 0;
            HpSteal = 0;
            STR = 0;
            INT = 0;
            AGI = 0;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
