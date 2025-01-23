using HS.Protobuf.SceneEntity;

namespace SceneServer.Core.Model.Item
{
    public class SceneItem : SceneEntity
    {
        private NetItemNode? m_netItemNode;

        public NetItemNode NetItemNode
        {
            get
            {
                return m_netItemNode;
            }
        }

    }
}
