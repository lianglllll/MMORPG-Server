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
        private AttrubuteData basic;    //人物本身的基础属性（定义+成长）
        private AttrubuteData equip;    //装备属性
        private AttrubuteData buff;     //Buff属性
        public AttrubuteData final;    //最终属性

        

        //角色的初始化属性
        public void Init(Actor actor)
        {
            basic = new AttrubuteData();
            equip = new AttrubuteData();
            buff = new AttrubuteData();
            final = new AttrubuteData();

            var define = actor.Define;
            int level = actor.info.Level;


            //初始化属性
            AttrubuteData Initial = new AttrubuteData();
            Initial.Speed = define.Speed;
            Initial.HPMax = define.HPMax;
            Initial.MPMax = define.MPMax;
            Initial.AD    = define.AD;
            Initial.AP    = define.AP;
            Initial.DEF   = define.DEF;
            Initial.MDEF  = define.MDEF;
            Initial.CRI   = define.CRI;
            Initial.CRD   = define.CRD;
            Initial.HitRate = define.HitRate;
            Initial.DodgeRate = define.DodgeRate;
            Initial.HpSteal = define.HpSteal;
            Initial.STR   = define.STR;
            Initial.INT   = define.INT;
            Initial.AGI   = define.AGI;

            //成长属性
            AttrubuteData Growth = new AttrubuteData();
            Growth.STR = define.GSTR * level;           //力量
            Growth.INT = define.GINT * level;           //智力
            Growth.AGI = define.GAGI * level;           //敏捷

            //基础属性(初始+成长)
            basic.Merge(Initial);
            basic.Merge(Growth);


            //todo 装备 


            //todo buff 




            //最终
            final.Merge(basic);
            final.Merge(equip);
            final.Merge(buff);

            //附加属性计算
            //1点力量 = 5 点生命值
            //1点智力 = 5 点魔攻
            AttrubuteData extra = new AttrubuteData();
            extra.HPMax = final.STR * 5;
            extra.AP = final.INT * 1;
            final.Merge(extra);
            /*
            Log.Information("初始属性{0}", basic);
            Log.Information("成长属性{0}", Growth);
            Log.Information("装备属性{0}", equip);
            Log.Information("buff属性{0}", buff);
            Log.Information("属性附加{0}", extra);
            Log.Information("最终属性{0}", final);
            */
        }

    }
}
