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
        public List<Spawner> m_spawners = new List<Spawner>();
        public void Init()
        {
            //根据当前场景加载对应的规则
            int sceneId = SceneManager.Instance.SceneId;
            var spawnDefines = StaticDataManager.Instance.spawnDefineDict.Values
                .Where(r => r.SpaceId == sceneId && r.SpawnNum > 0);
            foreach (var define in spawnDefines)
            {
                for (int i = 0; i < define.SpawnNum; i++)
                {
                    var spawner = new Spawner();
                    spawner.Init(define);
                    m_spawners.Add(spawner);
                }
            }
        }
        public void UnInit()
        {
            m_spawners.Clear();
        }
        public void Update(float deltaTime)
        {
            foreach(var spawner in m_spawners)
            {
                spawner.Update(deltaTime);
            }
        }
    }
}
