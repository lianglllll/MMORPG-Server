using Common.Summer.Core;
using HS.Protobuf.Common;
using HS.Protobuf.Backpack;
using HS.Protobuf.SceneEntity;

namespace SceneServer.Core.Model.Item
{
    public class SceneItem : SceneEntity
    {
        private NetItemNode m_netItemNode;

        public NetItemNode NetItemNode => m_netItemNode;

        public void Init(NetItemDataNode itemDataNode, Vector3Int pos, Vector3Int dir, Vector3Int scale)
        {
            base.Init(pos, dir, scale);
            m_netItemNode = new NetItemNode(); 
            m_netItemNode.NetItemDataNode = itemDataNode;

            var transform = new NetTransform();
            m_netItemNode.Transform = transform;
            transform.Position = new NetVector3();
            transform.Rotation = new NetVector3();
            transform.Scale = new NetVector3();
            m_netItemNode.Transform.Position = Position;
            m_netItemNode.Transform.Rotation = Rotation;
            m_netItemNode.Transform.Scale    = Scale;
        }
    }
}
