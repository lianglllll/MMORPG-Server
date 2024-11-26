using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat
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
            this.Speed  += attrubuteData.Speed;
            this.HPMax  += attrubuteData.HPMax;
            this.MPMax  += attrubuteData.MPMax;
            this.AD     += attrubuteData.AD;
            this.AP     += attrubuteData.AP;
            this.DEF    += attrubuteData.DEF;
            this.MDEF   += attrubuteData.MDEF;
            this.CRI    += attrubuteData.CRI;
            this.CRD    += attrubuteData.CRD;
            this.HitRate += attrubuteData.HitRate;
            this.DodgeRate += attrubuteData.DodgeRate;
            this.HpRegen += attrubuteData.HpRegen;
            this.HpSteal += attrubuteData.HpSteal;
            this.STR    += attrubuteData.STR;
            this.INT    += attrubuteData.INT;
            this.AGI    += attrubuteData.AGI;
        }

        //delete
        //减少属性
        public void Sub(AttrubuteData attrubuteData)
        {
            this.Speed  -= attrubuteData.Speed;
            this.HPMax  -= attrubuteData.HPMax;
            this.MPMax  -= attrubuteData.MPMax;
            this.AD     -= attrubuteData.AD;
            this.AP     -= attrubuteData.AP;
            this.DEF    -= attrubuteData.DEF;
            this.MDEF   -= attrubuteData.MDEF;
            this.CRI    -= attrubuteData.CRI;
            this.CRD    -= attrubuteData.CRD;
            this.HitRate -= attrubuteData.HitRate;
            this.DodgeRate -= attrubuteData.DodgeRate;
            this.HpRegen -= attrubuteData.HpRegen;
            this.HpSteal -= attrubuteData.HpSteal;
            this.STR    -= attrubuteData.STR;
            this.INT    -= attrubuteData.INT;
            this.AGI    -= attrubuteData.AGI;
        } 


        //属性清空
        public void Reset()
        {
            this.Speed  = 0;
            this.HPMax  = 0;
            this.MPMax  = 0;
            this.AD     = 0;
            this.AP     = 0;
            this.DEF    = 0;
            this.MDEF   = 0;
            this.CRI    = 0;
            this.CRD    = 0;
            this.HitRate = 0;
            this.DodgeRate = 0;
            this.HpRegen = 0;
            this.HpSteal = 0;
            this.STR    = 0;
            this.INT    = 0;
            this.AGI    = 0;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
