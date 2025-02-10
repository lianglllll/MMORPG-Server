using Common.Summer.Core;
using HS.Protobuf.SceneEntity;

namespace SceneServer.Core.Model
{
    public class SceneEntity
    {
        protected int m_entityId;
        private Vector3Int m_position;
        private Vector3Int m_rotation;
        private Vector3Int m_scale;

        public int EntityId
        {
            get { return m_entityId; }
            set
            {
                m_entityId = value;
            }
        }
        public Vector3Int Position
        {
            get { return m_position; }
            set
            {
                m_position = value;
            }
        }
        public Vector3Int Rotation
        {
            get { return m_rotation; }
            set
            {
                m_rotation = value;
            }
        }
        public Vector3Int Scale
        {
            get { return m_scale; }
            set
            {
                m_scale = value;
            }
        }
        public Vector2 AoiPos => new Vector2(m_position.x, m_position.z) / 1000;

        protected void Init(NetVector3 pos, Vector3Int rotation ,Vector3 scale)
        {
            m_position = pos;
            m_rotation = rotation;
            m_scale = scale;
        }
        public virtual void Update(float deltaTime)
        {

        }
    }
}
