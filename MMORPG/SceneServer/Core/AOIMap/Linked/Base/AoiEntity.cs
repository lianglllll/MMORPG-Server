
namespace SceneServer.Core.AOI
{
    /// <summary>
    /// AOI结果，描述了哪个人，在哪，他身边有谁。
    /// </summary>
    public sealed class AoiEntity
    {
        public readonly long Key;                   // entityId
        public AoiNode X;
        public AoiNode Y;
        // todo ViewEntity极其容易被修改 
        public HashSet<long> ViewEntity;            // 本次视野空间附近的人,不包括自己
        private HashSet<long> ViewEntityBak;        // 上次视野空间附近的人

        public IEnumerable<long> Leave => ViewEntityBak.Except(ViewEntity);
        public IEnumerable<long> Newly => ViewEntity.Except(ViewEntityBak);

        public AoiEntity(long key)
        {
            Key = key;
            ViewEntity = new HashSet<long>();
            ViewEntityBak = new HashSet<long>();
        }

        // tools
        public void RecordCurViewAndClear()
        {
            ViewEntityBak.Clear();

            var t3 = ViewEntity;
            ViewEntity = ViewEntityBak;
            ViewEntityBak = t3;
        }
        public List<long> GetViewEntityIds()
        {
            List<long> ids = ViewEntity.ToList();
            return ids;
        }
    }
}