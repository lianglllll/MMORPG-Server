using Common.Summer.Core;
using Common.Summer.Tools;
using HS.Protobuf.DBProxy.DBCharacter;
using SceneServer.Core.AOI;
using SceneServer.Core.Model;
using SceneServer.Core.Model.Actor;
using SceneServer.Utils;

namespace SceneServer.Core.Scene
{
    public class SceneManager : Singleton<SceneManager>
    {
        private SpaceDefine? m_sceneDefine;
        private Dictionary<int, SceneCharacter>? characterDict;         //<entityId，SceneCharacter>
        private AoiZone? m_aoiZone;                                        //AOI管理器：十字链表空间(unity坐标系)
        private Vector2 viewArea = new(Config.Server.aoiViewArea, Config.Server.aoiViewArea);

        public int SceneId => m_sceneDefine.SID;
        public AoiZone AoiZone => m_aoiZone;
        

        public void Init(int sceneId)
        {
            m_sceneDefine = StaticDataManager.Instance.sceneDefineDict[sceneId];
            characterDict = new();
            m_aoiZone = new AoiZone(0.001f, 0.001f);

            // 添加自循环
            Scheduler.Instance.AddTask(Update, Config.Server.updateHz, 0);
        }
        public void UnInit()
        {
            // 添加自循环
            Scheduler.Instance.RemoveTask(Update);
        }
        private void Update()
        {
        }

        public void AddSceneCharacter(DBCharacterNode dbChrNode)
        {
            // 创建sceneObj实例对象
            SceneCharacter sChr = new SceneCharacter();
            sChr.Init(dbChrNode);
        }
    }
}
