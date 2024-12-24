using GameServer.Model;
using Serilog;
using System.Collections.Generic;
using Common.Summer.Tools;

namespace GameServer.Manager
{
    /// <summary>
    /// 场景管理器
    /// </summary>
    public class SpaceManager:Singleton<SpaceManager>
    {
        //地图字典
        private Dictionary<int, Space> spaceDict = new Dictionary<int, Space>();

        /// <summary>
        /// 初始化地图信息
        /// </summary>
        public void init()
        {
            //获取datamanager中的spacedefin数据，将其加载到spacemanager中
            foreach(var kv in DataManager.Instance.spaceDefineDict)
            {
                spaceDict[kv.Key] = new Space(kv.Value);
                //Log.Information("初始化地图：{0}", kv.Value.Name);
            }
            //Log.Debug("==>共加载{0}个地图", DataManager.Instance.spaceDefineDict.Count);
        }

        /// <summary>
        /// 根据spaceid获取space
        /// </summary>
        /// <param name="spaceId"></param>
        /// <returns></returns>
        public Space GetSpaceById(int spaceId)
        {
            if (spaceDict.ContainsKey(spaceId))
            {
                return spaceDict[spaceId];
            }
            return null;
        }

        /// <summary>
        /// 推动每一个space的运行
        /// </summary>
        public void Update()
        {
            foreach(var s in spaceDict.Values)
            {
                s.Update();
            }
        }
    }
}
