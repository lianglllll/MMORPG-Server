using System.Collections.Generic;
using Common.Summer.Core;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Scene.Component
{
    // 每个地图中都有一个怪物管理器
    public class SceneMonsterManager
    {
        public Dictionary<int, SceneMonster> monsterDict = new(); // <entityid,Monster>

        public void Init()
        {

        }
        public void UnInit()
        {
            monsterDict.Clear();
        }

        public SceneMonster Create(int professionId, int level, Spawner spawner)
        {
            // 怪物初始化
            SceneMonster monster = new SceneMonster();
            SceneEntityManager.Instance.AddSceneEntity(monster);
            monster.Init(professionId, level, spawner);
            monster.NetActorNode.EntityId = monster.EntityId;

            // 添加到当前的mostermanager中管理
            monsterDict[monster.EntityId] = monster;

            // 显示到当前场景
            SceneManager.Instance.MonsterEnterScene(monster);

            monster.Init2();
            return monster;
        }


    }
}
