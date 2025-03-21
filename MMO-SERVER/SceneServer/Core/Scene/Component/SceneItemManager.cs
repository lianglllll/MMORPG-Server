using Common.Summer.Core;
using HS.Protobuf.Game.Backpack;
using SceneServer.Core.Model.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Scene.Component
{
    public class SceneItemManager
    {
        public Dictionary<int, SceneItem> itemEntityDict = new();       // <entityid, SceneItem>

        public void Init()
        {
        }
        public void UnInit()
        {
            throw new NotImplementedException();
        }

        public SceneItem CreateSceneItem(NetItemDataNode itemDataNode, Vector3Int pos, Vector3Int dir, Vector3Int scale)
        {
            var sceneItem = new SceneItem();
            sceneItem.Init(itemDataNode, pos, dir, scale);

            //添加到entityMananger中管理
            SceneEntityManager.Instance.AddSceneEntity(sceneItem);
            itemEntityDict[sceneItem.EntityId] = sceneItem;

            sceneItem.NetItemNode.EntityId = sceneItem.EntityId;
            sceneItem.NetItemNode.SceneId = SceneManager.Instance.SceneId;

            //显示到当前场景
            SceneManager.Instance.ItemEnterScene(sceneItem);

            return sceneItem;
        }

        public bool RemoveItem(SceneItem item)
        {
            bool result = false;
            if (!itemEntityDict.ContainsKey(item.EntityId))
            {
                goto End;
            }

            SceneEntityManager.Instance.RemoveSceneEntity(item.EntityId);
            itemEntityDict.Remove(item.EntityId);

            //场景中移除
            SceneManager.Instance.ItemExitScene(item.EntityId);

            result = true;
        End:
            return result;
        }

        public SceneItem GetEItemByEntityId(int entityId)
        {
            return itemEntityDict.GetValueOrDefault(entityId);
        }
    }
}
