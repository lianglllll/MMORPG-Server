using GameServer.Database;
using GameServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.core
{
    /// <summary>
    /// 用户会话
    /// </summary>
    class Session
    {
        public DbUser dbUser;
        public Character character;                                         //当前用户使用的角色
        public Space Space => character.currentSpace;                       //当前所在地图

    }
}
