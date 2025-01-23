using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Combat.Attrubute
{
    public class AttributeManager
    {
        private UnitDefine m_unitDefine;
        private AttrubuteData basic;        // 人物本身的基础属性
        private AttrubuteData growth;       // 人物等级成长属性
        public AttrubuteData equip;         // 装备属性
        public AttrubuteData buff;          // Buff属性
        private AttrubuteData extra;        // 附加属性
        public AttrubuteData final;         // 最终属性
        public AttrubuteData cur;           // 当前的属性

        public void Init(UnitDefine unitDefine, int level)
        {
            m_unitDefine = unitDefine;

            basic = new AttrubuteData();
            growth = new AttrubuteData();
            equip = new AttrubuteData();
            buff = new AttrubuteData();
            extra = new AttrubuteData();
            final = new AttrubuteData();
            cur = new AttrubuteData();

            //初始化属性
            basic.Speed = m_unitDefine.Speed;
            basic.MaxHP = m_unitDefine.HPMax;
            basic.MaxMP = m_unitDefine.MPMax;
            basic.AD = m_unitDefine.AD;
            basic.AP = m_unitDefine.AP;
            basic.DEF = m_unitDefine.DEF;
            basic.MDEF = m_unitDefine.MDEF;
            basic.CRI = m_unitDefine.CRI;
            basic.CRD = m_unitDefine.CRD;
            basic.HitRate = m_unitDefine.HitRate;
            basic.DodgeRate = m_unitDefine.DodgeRate;
            basic.HpSteal = m_unitDefine.HpSteal;
            basic.STR = m_unitDefine.STR;
            basic.INT = m_unitDefine.INT;
            basic.AGI = m_unitDefine.AGI;

            Reload(level);
        }

        // todo还需要细分
        public void Reload(int level)
        {
            final.Reset();

            // 人物等级成长属性
            growth.Reset();
            growth.STR = m_unitDefine.GSTR * level;           //力量
            growth.INT = m_unitDefine.GINT * level;           //智力
            growth.AGI = m_unitDefine.GAGI * level;           //敏捷

            // 装备 

            // buff 

            // 初次合并
            final.Merge(basic);
            final.Merge(growth);
            final.Merge(equip);
            final.Merge(buff);

            // 附加属性后合并
            //1点力量 = 5 点生命值
            //1点智力 = 2 点魔攻
            extra.MaxHP = (int)final.STR * 5;
            extra.AP = final.INT * 5;
            final.Merge(extra);

            // 当前状态刷新
            cur.Reset();
            cur.Merge(final);
        }
    }
}
