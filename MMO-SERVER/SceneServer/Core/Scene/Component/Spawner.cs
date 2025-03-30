using System.Text.RegularExpressions;
using Common.Summer.Core;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Scene.Component
{

    //刷怪对象,负责某个monster的刷新
    public class Spawner
    {
        public SpawnDefine m_define;
        public SceneMonster m_monster;

        private bool m_isReviving;          
        private float M_reviveTimePoint;

        public Vector3Int SpawnPoint { get; private set; }  //刷怪位置
        public Vector3Int SpawnDir { get; private set; }    //刷怪方向

        public void Init(SpawnDefine define)
        {
            m_define = define;
            m_isReviving = false;
            SpawnPoint = ParsePoint(define.Pos);
            SpawnDir = ParsePoint(define.Dir);
            // Log.Debug("New Spawner:场景[{0}],坐标[{1}],单位类型[{2}]", space.Name, SpawnPoint, define.TID);
            Spawn();
        }
        private void Spawn()
        {
            m_monster = SceneManager.Instance.SceneMonsterManager.Create(m_define.TID, m_define.Level, SpawnPoint, SpawnDir);
        }
        public void Update(float deltaTime)
        {
            // TODO:这里可以修改用事件来做把，当ai死亡就开一个定时事件，来复活它。
            if (m_monster != null && m_monster.IsDeath && !m_isReviving)
            {
                M_reviveTimePoint = MyTime.time + m_define.Period;
                m_isReviving = true;
            }
            if (m_isReviving && M_reviveTimePoint < MyTime.time)
            {
                m_monster?.Revive();
                m_isReviving = false;
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
