
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
        public HashSet<long> ViewEntity;            // 本次视野空间附近的人,不包括自己
        public HashSet<long> ViewEntityBak;         // 上次视野空间附近的人

        public IEnumerable<long> All => ViewEntity.Union(ViewEntityBak);
        public IEnumerable<long> Leave => ViewEntityBak.Except(ViewEntity);
        public IEnumerable<long> Newly => ViewEntity.Except(ViewEntityBak);

        public AoiEntity(long key)
        {
            Key = key;
            ViewEntity = new HashSet<long>();
            ViewEntityBak = new HashSet<long>();
        }

        public  void SwapViewEntity()
        {
            ViewEntityBak.Clear();
            var t3 = ViewEntity;
            ViewEntity = ViewEntityBak;
            ViewEntityBak = t3;
        }

    }
}