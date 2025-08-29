using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Common.Summer.Tools;


namespace SceneServer.Utils
{
    /// <summary>
    /// 用于导入json文件的数据
    /// </summary>
    public class StaticDataManager : Singleton<StaticDataManager>
    {
        //场景数据
        public Dictionary<int, SpaceDefine> sceneDefineDict = null;
        public Dictionary<int, UnitDefine> unitDefineDict = null;
        public Dictionary<int, SkillDefine> skillDefineDict = null;
        public Dictionary<int, BuffDefine> buffDefineDict = null;
        public Dictionary<int, SpawnDefine> spawnDefineDict = null;
        public Dictionary<int, RevivalPointDefine> revivalPointDefineDict = null;
        public Dictionary<int, WeaponSkillArsenalDefine> weaponSkillArsenalDefineDict = null;
        public Dictionary<int, LevelDefine> levelDefineDefineDict = null;


        //初始化，就是将文件中的数据读入
        public void Init()
        {
            //获取文件对象信息
            sceneDefineDict = Load<SpaceDefine>("SpaceDefine.json");
            unitDefineDict = Load<UnitDefine>("UnitDefine.json");
            skillDefineDict = Load<SkillDefine>("SkillDefine.json");
            buffDefineDict = Load<BuffDefine>("BuffDefine.json");
            spawnDefineDict = Load<SpawnDefine>("SpawnDefine.json");
            revivalPointDefineDict = Load<RevivalPointDefine>("RevivalPointDefine.json");
            weaponSkillArsenalDefineDict = Load<WeaponSkillArsenalDefine>("WeaponSkillArsenalDefine.json");
            levelDefineDefineDict = Load<LevelDefine>("LevelDefine.json");
        }

        //根据path加载解析json文件转换为dict
        public Dictionary<int, T> Load<T>(string filePath)
        {
            //获取exe文件所在目录的绝对路径
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            string txtFilePath = Path.Combine(exeDirectory, "..", "..", "..", "..", "Common", "Summer", "StaticData", "Data", filePath);

            //读取txt文件的内容
            string context = File.ReadAllText(txtFilePath);

            var settings = new JsonSerializerSettings
            {
                Converters = new JsonConverter[] {
                new FloatArrayConverter(),
                new IntArrayConverter(),
            }
            };

            return JsonConvert.DeserializeObject<Dictionary<int, T>>(context, settings);
        }

    }

    //自定义的JsonConverter,用于解决普通的JsonConverter无法转换float[]的问题
    public class FloatArrayConverter : JsonConverter<float[]>
    {
        public override float[] ReadJson(JsonReader reader, Type objectType, float[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.String)
            {
                string[] values = token.ToString().Replace("[", "").Replace("]", "").Split(',');
                float[] result = new float[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    float.TryParse(values[i], out result[i]);
                }
                return result;
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, float[] value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class IntArrayConverter : JsonConverter<int[]>
    {
        public override int[] ReadJson(JsonReader reader, Type objectType, int[] existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.String)
            {
                string[] values = token.ToString().Replace("[", "").Replace("]", "").Split(',');
                int[] result = new int[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    int.TryParse(values[i], out result[i]);
                }
                return result;
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, int[] value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

