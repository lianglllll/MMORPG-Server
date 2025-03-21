using System.Text.RegularExpressions;
using Common.Summer.Core;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Scene.Component
{

    //刷怪对象,负责某个monster的刷新
    public class Spawner
    {
        public SpawnDefine Define;
        public SceneMonster monster;
        private bool reviving;          //是否处于复活倒计时
        private float reviveTime;

        public Vector3Int SpawnPoint { get; private set; }  //刷怪位置
        public Vector3Int SpawnDir { get; private set; }    //刷怪方向

        public void Init(SpawnDefine define)
        {
            Define = define;
            SpawnPoint = ParsePoint(define.Pos);
            SpawnDir = ParsePoint(define.Dir);
            //Log.Debug("New Spawner:场景[{0}],坐标[{1}],单位类型[{2}]", space.Name, SpawnPoint, define.TID);
            Spawn();
            reviving = false;
        }
        private void Spawn()
        {
            monster = SceneManager.Instance.SceneMonsterManager.Create(Define.TID, Define.Level, SpawnPoint, SpawnDir);
        }
        public void Update(float deltaTime)
        {
            // TODO:这里可以修改用事件来做把，当ai死亡就开一个定时事件，来复活它。
            if (monster != null && monster.IsDeath && !reviving)
            {
                reviveTime = MyTime.time + Define.Period;
                reviving = true;
            }
            if (reviving && reviveTime < MyTime.time)
            {
                monster?.Revive();
                reviving = false;
            }

        }

        // 坐标转换
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
    }
}
