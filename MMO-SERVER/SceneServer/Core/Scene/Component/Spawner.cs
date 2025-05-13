using System.Globalization;
using System.Text.RegularExpressions;
using Common.Summer.Core;
using SceneServer.Core.Model.Actor;

namespace SceneServer.Core.Scene.Component
{

    // 刷怪对象,负责某个monster的刷新
    public class Spawner
    {
        private SpawnDefine     m_define;
        private SceneMonster    m_monster;
        private List<Vector3>   m_patrolPath;
        private Vector3         m_spawnPoint;       // 刷怪位置
        private Vector3         m_spawnDir;         // 刷怪方向

        private bool    m_isReviving;          
        private float   m_reviveTimePoint;

        #region GetSet
        public Vector3 SpawnPoint       => m_spawnPoint;
        public List<Vector3> PatrolPath => m_patrolPath;
        public string AIName => m_define.AI;
        #endregion
        #region 生命周期

        public void Init(SpawnDefine define)
        {
            m_define = define;
            m_isReviving = false;
            m_spawnPoint = ParsePoint(define.Pos);
            m_spawnDir = ParsePoint(define.Dir);
            if(m_define.PatrolType == 2)
            {
                m_patrolPath = ParsePatrolPath(m_define.PatrolPath);
            }
            Spawn();
        }
        private void Spawn()
        {
            m_monster = SceneManager.Instance.SceneMonsterManager.Create(m_define.TID, m_define.Level, this);
        }
        public void Update(float deltaTime)
        {
            // TODO:这里可以修改用事件来做把，当ai死亡就开一个定时事件，来复活它。
            if (m_monster != null && m_monster.IsDeath && !m_isReviving)
            {
                m_reviveTimePoint = MyTime.time + m_define.Period;
                m_isReviving = true;
            }
            if (m_isReviving && m_reviveTimePoint < MyTime.time)
            {
                m_monster?.Revive();
                m_isReviving = false;
            }

        }

        #endregion
        #region 工具
        // 坐标转换
        private Vector3 ParsePoint(string text)
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
            return Vector3.Zero;
        }
        private List<Vector3> ParsePatrolPath(string text)
        {
            var paths = new List<Vector3>();
            if (string.IsNullOrEmpty(text))
                return paths;

            // 按换行符分割字符串，并移除空行
            string[] lines = text.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // 按逗号分割每行的数据，并移除可能的空格
                string[] components = line.Split(',');
                if (components.Length != 3)
                    continue; // 跳过格式不正确的行

                // 解析为float（使用InvariantCulture避免文化差异问题）
                bool successX = float.TryParse(components[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                bool successY = float.TryParse(components[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                bool successZ = float.TryParse(components[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z);

                // 如果解析成功，添加到路径列表
                if (successX && successY && successZ)
                {
                    paths.Add(new Vector3(x, y, z));
                }
            }

            return paths;
        }
        #endregion
    }
}
