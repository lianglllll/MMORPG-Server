using GameServer.Core;
using GameServer.Model;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{

    //每个地图中都有一个怪物管理器
    public class MonsterManager
    {
        private Space curSpace;

        //<entityid,Monster>
        public Dictionary<int, Monster> monsterDict = new Dictionary<int, Monster>(); 
 
        public void Init(Space space)
        {
            this.curSpace = space;
        }

        public Monster Create(int tid,int level,Vector3Int pos,Vector3Int dir)
        {
            Monster monster = new Monster(tid,level, pos, dir);

            //添加到entity中管理
            EntityManager.Instance.AddEntity(curSpace.SpaceId, monster);

            //获取初始化的moster信息
            //这里和创建charcter不一样，我觉得应该创建一个新的类型NMoster来和Ncharacet区分//todo
 
            monster.AcotrId = monster.EntityId;//没啥用
            
            //添加到当前的mostermanager中管理
            monsterDict[monster.AcotrId] = monster;

            //显示到当前场景
            this.curSpace.MonsterJoin(monster);

            return monster;
        }

    }
}
