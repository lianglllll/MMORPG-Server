using SceneServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Scene.Component
{
    //刷怪管理器，每个场景都会有一个
    public class SpawnManager
    {
        public List<Spawner> spawners = new List<Spawner>();
        public void Init()
        {
            //根据当前场景加载对应的规则
            int sceneId = SceneManager.Instance.SceneId;
            var rules = StaticDataManager.Instance.spawnDefineDict.Values
                .Where(r => r.SpaceId == sceneId && r.SpawnNum > 0);
            foreach (var r in rules)
            {
                for (int i = 0; i < r.SpawnNum; i++)
                {
                    var spawner = new Spawner();
                    spawner.Init(r);
                    spawners.Add(spawner);
                }
            }
        }
        public void UnInit()
        {
            throw new NotImplementedException();
        }
        public void Update(float deltaTime)
        {
            foreach(var spawner in spawners)
            {
                spawner.Update(deltaTime);
            }
        }
    }
}
