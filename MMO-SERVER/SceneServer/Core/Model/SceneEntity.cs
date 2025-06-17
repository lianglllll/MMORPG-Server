using Common.Summer.Core;
using HS.Protobuf.Common;
using HS.Protobuf.SceneEntity;
using SceneServer.AOIMap.NineSquareGrid;

namespace SceneServer.Core.Model
{
    public class SceneEntity : IAOIUnit
    {
        protected int       m_entityId;
        private Vector3Int  m_position;  // 这里的vector3都是*1000倍的
        private Vector3Int  m_rotation;
        private Vector3Int  m_scale;
        private Vector2     m_aoiPos;

        #region GetSet
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
        public Vector2 AoiPos => m_aoiPos;
        #endregion

        #region 生命周期
        protected void Init(NetVector3 pos, Vector3Int rotation ,Vector3 scale)
        {
            m_position  = pos;
            m_rotation  = rotation;
            m_scale     = scale;
            m_aoiPos    = new Vector2(m_position.x, m_position.z) / 1000;
        }
        public virtual void Update(float deltaTime)
        {

        }
        #endregion

        #region Tools
        public virtual void SetTransform(NetTransform transform)
        {
            m_position = transform.Position;
            m_rotation = transform.Rotation;
            m_scale = transform.Scale;

            m_aoiPos.x = m_position.x / 1000.0f;
            m_aoiPos.y = m_position.z / 1000.0f;
        }
        public virtual NetTransform GetTransform()
        {
            return new NetTransform
            {
                Position = m_position,
                Rotation = m_rotation,
                Scale = m_scale,
            };
        }
        #endregion

        #region AOI
        public virtual void OnUnitEnter(IAOIUnit unit)
        {
        }
        public virtual void OnUnitLeave(IAOIUnit unit)
        {
        }
        public virtual void OnPosError()
        {
        }
        #endregion
    }
}
