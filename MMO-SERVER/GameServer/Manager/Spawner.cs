using GameServer.Core;
using GameServer.Model;
using Serilog;
using GameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Summer.GameServer;

namespace GameServer.Manager
{

    //刷怪对象,负责某个monster的刷新
    public class Spawner
    {
        public SpawnDefine Define;
        public Space Space;
        public Monster monster;
        private bool reviving;          //是否处于复活倒计时
        private float reviveTime;       

        public Vector3Int SpawnPoint { get; private set; }  //刷怪位置
        public Vector3Int SpawnDir { get; private set; }    //刷怪方向

        public Spawner(SpawnDefine define, Space space )
        {
            Define = define;
            Space = space;
            SpawnPoint = ParsePoint(define.Pos);
            SpawnDir = ParsePoint(define.Dir);
            //Log.Debug("New Spawner:场景[{0}],坐标[{1}],单位类型[{2}]", space.Name, SpawnPoint, define.TID);
            this.spawn();
            reviving = false;
        }

        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private Vector3Int ParsePoint(string text)
        {
            //@""表示不处理转义字符
            string pattern = @"\[(\d+),(\d+),(\d+)\]";
            Match match = Regex.Match(text, pattern);
            if (match.Success)
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                int z = int.Parse(match.Groups[3].Value);
                return new Vector3(x, y, z);
            }
            return Vector3Int.zero;
        }

        /// <summary>
        /// 孵化怪物
        /// </summary>
        private void spawn()
        {
            this.monster = this.Space.monsterManager.Create(Define.TID,Define.Level, SpawnPoint, SpawnDir);
        }

        /// <summary>
        /// 这里可以修改用事件来做把，当ai死亡就开一个定时事件，来复活它。
        /// </summary>
        public void Update()
        {
            if(monster != null && monster.IsDeath && !reviving)
            {
                this.reviveTime = MyTime.time + Define.Period;
                reviving = true;
            }
            if(reviving && reviveTime < MyTime.time)
            {
                this.monster?.Revive();
                reviving = false;
            }

        }
    }
}
