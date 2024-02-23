using GameServer.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Combat
{
    //战斗属性
    public class Attributes
    {
        private AttrubuteData basic;        //人物本身的基础属性
        private AttrubuteData growth;       //人物等级成长属性
        public AttrubuteData equip;        //装备属性
        public AttrubuteData buff;         //Buff属性
        public AttrubuteData final;         //最终属性

        public Actor owner { get; private set; }

        /// <summary>
        ///actor的初始化属性
        /// </summary>
        /// <param name="actor"></param>
        public void Init(Actor actor)
        {

            owner = actor;
            var define = actor.Define;
            int level = actor.info.Level;


            basic = new AttrubuteData();
            growth = new AttrubuteData();
            equip = new AttrubuteData();
            buff = new AttrubuteData();
            final = new AttrubuteData();

            //初始化属性
            basic.Speed = define.Speed;
            basic.HPMax = define.HPMax;
            basic.MPMax = define.MPMax;
            basic.AD    = define.AD;
            basic.AP    = define.AP;
            basic.DEF   = define.DEF;
            basic.MDEF  = define.MDEF;
            basic.CRI   = define.CRI;
            basic.CRD   = define.CRD;
            basic.HitRate = define.HitRate;
            basic.DodgeRate = define.DodgeRate;
            basic.HpSteal = define.HpSteal;
            basic.STR   = define.STR;
            basic.INT   = define.INT;
            basic.AGI   = define.AGI;

            Reload();

            /*
            Log.Information("初始属性{0}", basic);
            Log.Information("成长属性{0}", Growth);
            Log.Information("装备属性{0}", equip);
            Log.Information("buff属性{0}", buff);
            Log.Information("属性附加{0}", extra);
            Log.Information("最终属性{0}", final);
            */
        }


        /// <summary>
        /// 重新加载属性,比如说升级了、穿装备了，这时候就需要重新加载属性
        /// </summary>
        public void Reload()
        {
            var define = owner.Define;
            int level = owner.info.Level;

            //等级成长属性
            growth.Reset();
            growth.STR = define.GSTR * level;           //力量
            growth.INT = define.GINT * level;           //智力
            growth.AGI = define.GAGI * level;           //敏捷

            //todo 装备 

            //todo buff 

            //初次合并
            final.Reset();
            final.Merge(basic);
            final.Merge(growth);
            final.Merge(equip);
            final.Merge(buff);

            //附加属性
            //1点力量 = 5 点生命值
            //1点智力 = 2 点魔攻
            AttrubuteData extra = new AttrubuteData();
            extra.HPMax = final.STR * 5;
            extra.AP = final.INT * 5;

            //最终合并
            final.Merge(extra);

            //告诉actor
            owner.SyncAttributes();

        }
    }
}
