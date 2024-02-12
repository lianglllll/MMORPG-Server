using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{

    //刷怪管理器，每个场景都会有一个
    public class SpawnManager
    {
        public List<Spawner> ruleList = new List<Spawner>();
        public Space Space { get; set; }

        public SpawnManager()
        {
        }

        public void Init(Space space)
        {
            this.Space = space;
            //根据当前场景加载对应的规则
            var rules = DataManager.Instance.spawnDefineDict.Values
                .Where(r => r.SpaceId == space.SpaceId);
            foreach(var r in rules)
            {
                ruleList.Add(new Spawner(r, space));
            }
        }

        /// <summary>
        /// 推动刷怪器
        /// </summary>
        public void Update()
        {
            ruleList.ForEach(r => r.Update());
        }

    }
}
