using Proto;
using Summer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Core;


namespace GameServer.Model
{
    /// <summary>
    /// 在MMO世界地图进行同步的实体
    /// 就是一切动态的对象
    /// </summary>
    public class Entity
    {
        private Vector3Int position;                //位置
        private Vector3Int direction;               //方向
        private NetEntity netObj;                   //网络对象  
        private long _lastUpdate;                   //最后一次更新位置的时间戳

        /// <summary>
        /// entityid存放在netObj中,这个id的赋值在entitymanager中完成
        /// </summary>
        public int EntityId { 
            get { return netObj.Id; } 
            set { netObj.Id = value; } 
        }

        /// <summary>
        /// entity的位置属性
        /// </summary>
        public Vector3Int Position
        {
            get { return position; }
            set {
                position = value;
                netObj.Position = value;  //重载了'='，prot是Vec3  服务器是VectorInt
                _lastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// entity的方向属性
        /// </summary>
        public Vector3Int Direction
        {
            get { return direction; }
            set {
                direction = value;
                netObj.Direction = value;
            }
        }
        
        /// <summary>
        /// NetEntity 网络对象属性
        /// </summary>
        public Proto.NetEntity EntityData
        {
            get {
                return netObj;
            }
            set
            {
                //id的话不更新也行
                Position = value.Position;
                Direction = value.Direction;
            }
        }

        /// <summary>
        /// 构造函数
        /// 此时entityid 没有
        /// speed 没有
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="dir"></param>
        public Entity(Vector3Int pos, Vector3Int dir)
        {
            //赋值的同时，设置网络对象
            netObj = new NetEntity();
            Position = pos;
            Direction = dir;
            //this.EntityId;        entityid 等待entitymanager分配id
        }

        /// <summary>
        /// 推动实体在mmo世界的运行
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// 获取位置更新时间间隔
        /// </summary>
        public float PositionUpdateTimeDistance
        {
            get { return (DateTimeOffset.Now.ToUnixTimeMilliseconds() - _lastUpdate) * 0.001f; }
        }
    }
}

