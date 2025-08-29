using Common.Summer.Core;
using Serilog;

namespace SceneServer.AOIMap.NineSquareGrid
{
    public interface IAOIUnit
    {
        //  九宫格格子单位
        public Vector2 AoiPos { get; }          // 这个单位在aoi体系下的二维坐标
        public void OnUnitEnter(IAOIUnit unit);
        public void OnUnitLeave(IAOIUnit unit);
        public void OnPosError();
    }
    public class Grid
    {
        public int GID { get; private set; }
        private HashSet<IAOIUnit> aoiSet;           // 我们需要同步的单位：角色、怪物、npc、物品
        private ReaderWriterLockSlim pIDLock;       // 读写锁

        public Grid(int gridId)
        {
            GID = gridId;
            aoiSet = new();
            pIDLock = new();
        }
        public void AddAOIUnit(IAOIUnit obj)
        {
            pIDLock.EnterReadLock();
            try
            {
                if (!aoiSet.Contains(obj))
                {
                    aoiSet.Add(obj);
                }
            }
            finally
            {
                pIDLock.ExitReadLock();
            }
        }
        public void RemoveAOIUnit(IAOIUnit obj)
        {
            pIDLock.EnterReadLock();
            try
            {
                aoiSet.Remove(obj);
            }
            finally
            {
                pIDLock.ExitReadLock();
            }

        }
        public List<IAOIUnit> GetAOIUnits()
        {
            pIDLock.EnterReadLock();
            try
            {
                return aoiSet.ToList();
            }
            finally
            {
                pIDLock.ExitReadLock();
            }
        }
    }

    public class AOIManager<T> where T : IAOIUnit
    {
        private int m_minX;
        private int m_maxX;
        private int m_minY;
        private int m_maxY;
        private int m_xCnts;
        private int m_yCnts;
        private int m_cellSize = 50;        // 每个格子的尺寸 
        public Dictionary<int, Grid> Grids { get; private set; }

        private static int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
        private static int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

        #region GetSet
        public int MinX => m_minX;
        public int MaxX => m_maxX;
        public int MinY => m_minY;
        public int MaxY => m_maxY;
        public int XCnts => m_xCnts;
        public int YCnts => m_yCnts;
        #endregion

        #region 生命周期
        public AOIManager(int minX, int minY, int maxX, int maxY)
        {
            m_minX = minX;
            m_minY = minY;
            m_maxX = maxX;
            m_maxY = maxY;
            // 计算水平方向格子数量，向上取整
            m_xCnts = (int)Math.Ceiling((maxX - minX) / (float)m_cellSize);
            // 计算垂直方向格子数量，向上取整
            m_yCnts = (int)Math.Ceiling((maxY - minY) / (float)m_cellSize);
            // 给aoi初始化区域中所有的格子进行编号和初始化
            Grids = new Dictionary<int, Grid>();
            for (int y = 0; y < YCnts; y++)
            {
                for (int x = 0; x < XCnts; x++)
                {
                    int gid = y * XCnts + x;
                    Grids[gid] = new Grid(gid);
                }
            }
        }
        #endregion

        #region 功能
        public void Enter(T obj)
        {
            var pos = obj.AoiPos;
            var grid = _GetGridByPos(pos.x, pos.y);
            if (grid == null)
            {
                obj.OnPosError();
                return;
            }
            grid.AddAOIUnit(obj);
            _Move(obj, -9, grid.GID);
        }
        public void Exit(T obj)
        {
            var pos = obj.AoiPos;
            var grid = _GetGridByPos(pos.x, pos.y);
            if (grid == null)
            {
                obj.OnPosError();
                return;
            }
            grid.RemoveAOIUnit(obj);
            _Move(obj, grid.GID, -9);
        }
        public bool Move(T unit, Vector2 oldPos, Vector2 curPos)
        {
            var oldGid = _GetGridIdByPos(oldPos.x, oldPos.y);
            var curGid = _GetGridIdByPos(curPos.x, curPos.y);
            return _Move(unit, oldGid, curGid);
        }
        private bool _Move(T unit, int oldGid, int currGid)
        {
            var oldGrid = Grids.GetValueOrDefault(oldGid);
            var currGrid = Grids.GetValueOrDefault(currGid);

            var isCrossGrid = false;

            if(oldGid == currGid)
            {
                goto End;
            }

            oldGrid?.RemoveAOIUnit(unit);
            currGrid?.AddAOIUnit(unit);

            // 计算出对象去除和添加的感兴趣格子
            var oldArea = _GetSurroundGrids(oldGid);
            var curArea = _GetSurroundGrids(currGid);
            var intersectKeys = oldArea.Keys.Intersect(curArea.Keys);
            var lostKeys = oldArea.Keys.Except(intersectKeys);
            var bornKeys = curArea.Keys.Except(intersectKeys);

            // 离开的格子
            foreach (var key in lostKeys)
            {
                var lostGrid = Grids[key];
                var list = lostGrid.GetAOIUnits();
                foreach (var item in list)
                {
                    item.OnUnitLeave(unit); //告诉item，unit离开我们的视野了
                    unit.OnUnitLeave(item); //告诉unit,item离开我们的视野了
                }
            }

            // 新增的格子
            foreach (var key in bornKeys)
            {
                var bornGrid = Grids[key];
                var list = bornGrid.GetAOIUnits();
                foreach (var item in list)
                {
                    item.OnUnitEnter(unit); // 告诉item，unit进入我们的视野了
                    unit.OnUnitEnter(item); // 告诉unit,item进入我们的视野了
                }
            }

            isCrossGrid = true;
        End:
            return isCrossGrid;
        }
        #endregion

        #region tools
        private Dictionary<int, Grid> _GetSurroundGrids(float x, float y)
        {
            return _GetSurroundGrids(_GetGridIdByPos(x, y));
        }
        private List<Grid> _Get9Grids(int gid)
        {
            return _GetSurroundGrids(gid).Values.ToList();
        }
        private Dictionary<int, Grid> _GetSurroundGrids(int gridId)
        {
            Dictionary<int, Grid> result = new();
            if (!Grids.ContainsKey(gridId))
            {
                return result;
            }

            // 先加自己
            result[gridId] = Grids[gridId];

            int x = gridId % XCnts;
            int y = gridId / YCnts;

            for (int i = 0; i < 8; i++)
            {
                int newX = x + dx[i];
                int newY = y + dy[i];

                // 注意边界问题
                if (newX >= 0 && newX < XCnts && newY >= 0 && newY < YCnts)
                {
                    int _gid = newY * XCnts + newX;
                    if (Grids.TryGetValue(_gid, out var gird))
                    {
                        result[_gid] = gird;
                    }
                }
            }
            return result;
        }

        private int _GetGridIdByPos(float x, float y)
        {
            // 检查是否超出地图范围（MinX ≤ x ≤ MaxX）
            if (x < MinX || x >= MaxX || y < MinY || y >= MaxY)
                return -9; // 超出范围，返回异常值

            // 计算格子索引（确保 x 和 y 在合法范围内）
            int idx = (int)((x - MinX) / m_cellSize);
            int idy = (int)((y - MinY) / m_cellSize);

            // 返回格子ID
            return idy * XCnts + idx;
        }
        private Grid _GetGridByPos(float posX, float posY)
        {
            var gid = _GetGridIdByPos(posX, posY);
            if (!Grids.TryGetValue(gid, out var gird))
            {
                Log.Warning($"\n GetGridByPos is null: gid={gid}");
            }
            return gird;
        }

        public List<T> GetAOIUnits(Vector2 vec)
        {
            return GetAOIUnits(vec.x, vec.y);
        }
        public List<T> GetAOIUnits(float x, float y)
        {
            int gId = _GetGridIdByPos(x, y);
            var grids = _GetSurroundGrids(gId);
            var result = new List<T>();

            foreach (var grid in grids.Values)
            {
                result.AddRange(grid.GetAOIUnits().OfType<T>());
            }

            return result;
        }

        public override string ToString()
        {
            // 打印aoi信息
            string s = $"AOIManager:\n" +
                       $"MinX:{MinX},MaxX:{MaxX},CntsX:{XCnts}\n" +
                       $"MinY:{MinY},MaxX:{MaxY},CntsX:{YCnts}\n";

            // 打印格子的信息
            foreach (var grid in Grids.Values)
            {
                s += $"{grid}\n";
            }

            return s;
        }
        #endregion
    }
}

