using Serilog;
using Common.Summer.Core;
using SceneServer.Core.Model;
using SceneServer.Utils;
using SceneServer.Core.Scene.Component;

namespace SceneServer.Core.AOI
{
    public sealed class AoiZone
    {
        private readonly AoiLinkedList _xLinks;
        private readonly AoiLinkedList _yLinks;
        private readonly Dictionary<long, AoiEntity> _entityList = new Dictionary<long, AoiEntity>();   // <entityId,AoiEntity>

        // 构造函数
        public AoiZone()
        {
            _xLinks = new AoiLinkedList();
            _yLinks = new AoiLinkedList();
        }
        public AoiZone(float xLinksLimit, float yLinksLimit)
        {
            _xLinks = new AoiLinkedList(limit: xLinksLimit);
            _yLinks = new AoiLinkedList(limit: xLinksLimit);
        }
        public AoiZone(int maxLayer, float xLinksLimit, float yLinksLimit)
        {
            _xLinks = new AoiLinkedList(maxLayer, xLinksLimit);
            _yLinks = new AoiLinkedList(maxLayer, yLinksLimit);
        }

        // 进入aoi空间
        public AoiEntity Enter(SceneEntity entity)
        {
            var point = entity.AoiPos;
            return Enter(entity.EntityId, point.x, point.y);
        }
        private AoiEntity Enter(long key, float x, float y)
        {
            AoiEntity result;

            if (_entityList.TryGetValue(key, out var aoiEntity))
            {
                result = aoiEntity;
                goto End;
            }

            result = new AoiEntity(key);
            result.X = _xLinks.Add(x, result);
            result.Y = _yLinks.Add(y, result);
            _entityList.Add(key, result);

        End:
            return result;
        }
        public AoiEntity Enter_ReturnCurView(long key, float x, float y, Vector2 area, out HashSet<long> enter)
        {
            var entity = Enter(key, x, y);
            Refresh_ReturnCurView(key, area, out enter);
            return entity;
        }
        public AoiEntity Enter_ReturnCurView_IncludingMyself(long key, float x, float y, Vector2 area, out HashSet<long> enter)
        {
            var entity = Enter(key, x, y);
            Refresh_ReturnCurView_IncludingMyself(key, area, out enter);
            return entity;
        }

        // 退出aoi空间
        public void Exit(long key)
        {
            if (!_entityList.TryGetValue(key, out var entity)) return;

            Exit(entity);
        }
        private void Exit(AoiEntity node)
        {
            _xLinks.Remove(node.X);
            _yLinks.Remove(node.Y);
            _entityList.Remove(node.Key); ;
        }

        // 用于刷新我们的视野范围
        public AoiEntity Refresh(long key, Vector2 area)
        {
            AoiEntity result;
            if (!_entityList.TryGetValue(key, out result)){
                goto End;
            }
            Find(result, ref area);
        End:
            return result;
        }
        public AoiEntity UpdatePos_Refresh(long key, float x, float y, Vector2 area)
        {
            // 用于更新坐标信息并且刷新我们的视野范围(若坐标没有变化则不刷新)
            if (!_entityList.TryGetValue(key, out var entity)) return null;

            var isFind = false;

            if (Math.Abs(entity.X.Value - x) > 0)
            {
                isFind = true;
                _xLinks.Move(entity.X, ref x);
            }

            if (Math.Abs(entity.Y.Value - y) > 0)
            {
                isFind = true;
                _yLinks.Move(entity.Y, ref y);
            }

            /*            if (isFind) {
                            Find(entity, ref area);
                        }*/
            Find(entity, ref area);

            return entity;
        }
        public AoiEntity Refresh_ReturnCurView(long key, Vector2 area, out HashSet<long> enter)
        {
            var entity = Refresh(key, area);
            enter = entity.ViewEntity;
            return entity;
        }
        public AoiEntity Refresh_ReturnCurView_IncludingMyself(long key, Vector2 area, out HashSet<long> enter)
        {
            var entity = Refresh_ReturnCurView(key, area, out enter);
            enter?.Add(key);
            return entity;
        }
        public AoiEntity UpdatePos_Refresh_ReturnCurView(long key, float x, float y, Vector2 area, out HashSet<long> enter)
        {
            var entity = UpdatePos_Refresh(key, x, y, area);
            enter = entity?.ViewEntity;
            return entity;
        }
        public AoiEntity Update_Refresh_ReturnCurView_IncludingMyself(long key, float x, float y, Vector2 area, out HashSet<long> enter)
        {
            var entity = UpdatePos_Refresh_ReturnCurView(key, x, y, area, out enter);
            enter?.Add(key);
            return entity;
        }

        // 寻找范围内的entity
        private void Find(AoiEntity node, ref Vector2 area)
        {
            // SwapViewEntity(ref node.ViewEntity, ref node.ViewEntityBak);
            node.RecordViewAndClear();

            #region xLinks

            // 向链表的左右两边查找
            for (var i = 0; i < 2; i++)
            {
                AoiNode cur = i == 0 ? node.X.Right : node.X.Left;

                while (cur != null)
                {
                    if (Math.Abs(Math.Abs(cur.Value) - Math.Abs(node.X.Value)) > area.x)
                    {
                        break;
                    }

                    if (Math.Abs(Math.Abs(cur.Entity.Y.Value) - Math.Abs(node.Y.Value)) <= area.y)
                    {
                        if (Distance(new Vector2(node.X.Value, node.Y.Value), new Vector2(cur.Entity.X.Value, cur.Entity.Y.Value)) <= area.x)
                        {
                            try
                            {
                                node.ViewEntity.Add(cur.Entity.Key);
                            }
                            catch (Exception e)
                            {
                                Log.Error("AoiZone.Fine:{0}",e.ToString());
                                throw;
                            }
                        }
                    }

                    cur = i == 0 ? cur.Right : cur.Left;
                }
            }

            #endregion

            #region yLinks

            for (var i = 0; i < 2; i++)
            {
                var cur = i == 0 ? node.Y.Right : node.Y.Left;

                while (cur != null)
                {
                    if (Math.Abs(Math.Abs(cur.Value) - Math.Abs(node.Y.Value)) > area.y)
                    {
                        break;
                    }

                    if (Math.Abs(Math.Abs(cur.Entity.X.Value) - Math.Abs(node.X.Value)) <= area.x)
                    {
                        if (Distance(
                            new Vector2(node.X.Value, node.Y.Value),
                            new Vector2(cur.Entity.X.Value, cur.Entity.Y.Value)) <= area.x)
                        {
                            
                            try
                            {
                                node.ViewEntity.Add(cur.Entity.Key);
                            }
                            catch (Exception e)
                            {
                                Log.Error("AoiZone.Fine:{0}",e.ToString());
                                throw;
                            }
                        }
                    }

                    cur = i == 0 ? cur.Right : cur.Left;
                }
            }

            #endregion
        }

        // tools
        public AoiEntity GetAoiEntityById(long key)
        {
            if (_entityList.TryGetValue(key, out var entity))
            {
                return entity;
            }
            return null;
        }
        private double Distance(Vector2 a, Vector2 b)
        {
            return Math.Pow((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y), 0.5);
        }
        public IEnumerable<SceneEntity> FindViewEntity(int key, bool includeSelf = false)
        {
            // 查找附近Entity对象，范围通过config配置
            var area = Config.Server.aoiViewArea;
            var handle = Refresh(key, new Vector2(area, area));
            // handle有可能为空
            if (handle == null) return new HashSet<SceneEntity>();
            var units = SceneEntityManager.Instance.GetSceneEntitiesByIds(handle.ViewEntity);
            if (includeSelf)
            {
                units.Add(SceneEntityManager.Instance.GetSceneEntityById(key));
            }
            return units;
        }
        private static void SwapViewEntity(ref HashSet<long> viewEntity, ref HashSet<long> viewEntityBak)
        {
            viewEntityBak.Clear();
            var t3 = viewEntity;
            viewEntity = viewEntityBak;
            viewEntityBak = t3;
        }
    }
}