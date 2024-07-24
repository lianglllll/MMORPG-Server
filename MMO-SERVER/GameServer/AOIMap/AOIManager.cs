using GameServer;
using GameServer.Model;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;

namespace AOIMap;

/// <summary>
/// 格子里的单位
/// </summary>
public interface IAOIUnit
{
    public Vector2 AoiPos { get; }          //这个单位在aoi体系下的二维坐标
    public void OnUnitEnter(IAOIUnit unit);
    public void OnUnitLeave(IAOIUnit unit);
    public void OnPosError();
}

/// <summary>
/// AOI区域管理模块，每个场景都需要一个
/// </summary>
/// <typeparam name="T"></typeparam>
public class AOIManager<T> where T : IAOIUnit
{

    // 左
    public int MinX { get; private set; }

    // 右
    public int MaxX { get; private set; }

    // 下
    public int MinY { get; private set; }

    // 上
    public int MaxY { get; private set; }
    
    // X方向格子的数量
    public int CntsX { get; private set; }

    // Y方向格子的数量
    public int CntsY { get; private set; }

    //每个格子的尺寸 
    private int cellSize = 50;


    // 9个方向的位置偏移,除了自己这个点
    static int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
    static int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };


    // 当前区域中有哪些格子
    public Dictionary<int, Grid> Grids { get; private set; }


    /// <summary>
    /// 构造函数，初始化一个AOI区域管理模块
    /// </summary>
    /// <param name="minX"></param>
    /// <param name="minY"></param>
    /// <param name="maxX"></param>
    /// <param name="maxY"></param>
    public AOIManager(int minX, int minY, int maxX, int maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        //计算水平方向格子数量，向上取整
        CntsX = (int)Math.Ceiling((maxX-minX)/(float)cellSize);
        //计算垂直方向格子数量，向上取整
        CntsY = (int)Math.Ceiling((maxY-minY)/(float)cellSize);

        // 给aoi初始化区域中所有的格子进行编号和初始化
        Grids = new Dictionary<int,Grid>();
        for (int y = 0; y < CntsY; y++)
        {
            for (int x = 0; x < CntsX; x++)
            {
                int gid = y * CntsX + x;
                Grids[gid] = new Grid(gid);
                //Log.Information("==> {0}", Grids[gid]);
            }
        }
    }

    /// <summary>
    /// 打印格子的信息
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        // 打印aoi信息
        string s = $"AOIManager:\n" +
                   $"MinX:{MinX},MaxX:{MaxX},CntsX:{CntsX}\n" +
                   $"MinY:{MinY},MaxX:{MaxY},CntsX:{CntsY}\n";

        // 打印格子的信息
        foreach (var grid in Grids.Values)
        {
            s += $"{grid}\n";
        }

        return s;
    }

    /// <summary>
    /// 获取附近9个格子
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public Dictionary<int, Grid> GetSurroundGrids(float x, float y)
    {
        return GetSurroundGrids(GetGidByPos(x, y));
    }
    public List<Grid> Get9Grids(int gid)
    {
        return GetSurroundGrids(gid).Values.ToList();
    }
    public Dictionary<int,Grid> GetSurroundGrids(int gID)
    {
        Dictionary<int, Grid> result = new();
        if (!Grids.ContainsKey(gID))
        {
            return result;
        }

        //先加自己
        result[gID] = Grids[gID];

        int x = gID % CntsX;
        int y = gID / CntsY;

        for (int i = 0; i < 8; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            //注意边界问题
            if (newX >= 0 && newX < CntsX && newY >= 0 && newY < CntsY)
            {
                int _gid = newY * CntsX + newX;
                if(Grids.TryGetValue(_gid,out var gird))
                {
                    result[_gid] = gird;
                }
            }
        }
        return result;
    }



    /// <summary>
    /// 通过x、y横纵轴坐标得到当前的GID格子编号
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetGidByPos(float x, float y)
    {
        int idx = ((int)x - MinX) / cellSize;
        int idy = ((int)y - MinY) / cellSize;
        return idy * CntsX + idx;
    }



    /// <summary>
    /// 通过x、y横纵轴坐标得到当前的GID格子
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    /// <returns></returns>
    public Grid GetGridByPos(float posX, float posY)
    {
        var gid = GetGidByPos(posX, posY);
        if (!Grids.TryGetValue(gid, out var gird))
        {
            Console.WriteLine($"\n GetGridByPos is null: gid={gid}");
        }
        return gird;
    }



    /// <summary>
    /// 通过坐标得到周边9宫格内全部的角色
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public List<T> GetEntities(Vector2 vec)
    {
        return GetEntities(vec.x, vec.y);
    }
    public List<T> GetEntities(float x, float y)
    {
        int gID = GetGidByPos(x, y);
        var grids = GetSurroundGrids(gID);
        var result = new List<T>();

        foreach (var grid in grids.Values)
        {
            result.AddRange(grid.GetEntities().OfType<T>());
        }

        return result;
    }



    /// <summary>
    /// 玩家进入场景格子（出生、在地图上初始化-进入场景） 通知格子周围玩家
    /// </summary>
    /// <param name="obj"></param>
    public void Enter(T obj)
    {
        var pos = obj.AoiPos;
        var grid = GetGridByPos(pos.x, pos.y);
        if(grid == null)
        {
            obj.OnPosError();
            return;
        }
        grid.Add(obj);
        Move(obj, -9, grid.GID);
    }

    //玩家从格子删除（离开场景、死亡） 通知格子周围玩家
    public void Leave(T obj)
    {
        var pos = obj.AoiPos;
        var grid = GetGridByPos(pos.x, pos.y);
        if(grid == null)
        {
            obj.OnPosError();
            return;
        }
        grid.Remove(obj);
        Move(obj, grid.GID, -9);
    }

    /// <summary>
    /// 单位在aoi范围内移动
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="oldPos"></param>
    /// <param name="curPos"></param>
    /// <returns></returns>
    public bool Move(T unit, Vector2 oldPos, Vector2 curPos)
    {
        var oldGid = GetGidByPos(oldPos.x, oldPos.y);
        var curGid = GetGidByPos(curPos.x, curPos.y);
        return Move(unit, oldGid, curGid);
    }
    private bool Move(T unit,int oldGid, int currGid)
    {
        var oldGrid = Grids.GetValueOrDefault(oldGid);
        var currGrid = Grids.GetValueOrDefault(currGid);
        
        var isCrossGrid = false;

        if (oldGrid != currGrid)
        {
            oldGrid?.Remove(unit);
            currGrid?.Add(unit);

            //计算出对象去除和添加的感兴趣格子
            var oldArea = GetSurroundGrids(oldGid);
            var curArea = GetSurroundGrids(currGid);
            var intersectKeys = oldArea.Keys.Intersect(curArea.Keys);
            var lostKeys = oldArea.Keys.Except(intersectKeys);
            var bornKeys = curArea.Keys.Except(intersectKeys);

            //离开的格子
            foreach (var key in lostKeys)
            {
                var lostGrid = Grids[key];
                var list = lostGrid.GetEntities();
                foreach (var item in list)
                {
                    item.OnUnitLeave(unit); //告诉item，unit离开我们的视野了
                    unit.OnUnitLeave(item); //告诉unit,item离开我们的视野了
                }
            }

            //新增的格子
            foreach (var key in bornKeys)
            {
                var bornGrid = Grids[key];
                var list = bornGrid.GetEntities();
                foreach (var item in list)
                {
                    item.OnUnitEnter(unit); //告诉item，unit进入我们的视野了
                    unit.OnUnitEnter(item); //告诉unit,item进入我们的视野了
                }
            }

            isCrossGrid = true;
        }

        return isCrossGrid;
    }

    
}