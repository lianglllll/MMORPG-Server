using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Common.Summer.Tools;
using ControlCenter.Core;
using HS.Protobuf.Common;


namespace ControlCenter.Utils
{
    /// <summary>
    /// 用于导入json文件的数据
    /// </summary>
    public class StaticDataManager : Singleton<StaticDataManager>
    {
        public Dictionary<int, ServerInfoNode> serverInfoNodeDict = null;

        public void Init()
        {
            //获取文件对象信息
            serverInfoNodeDict = Load<ServerInfoNode>("test.json");
        }

        public Dictionary<int, T> Load<T>(string filePath)
        {
            //获取exe文件所在目录的绝对路径
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            string txtFilePath = Path.Combine(exeDirectory, filePath);

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
        public void Save(string filePath)
        {
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);
            string jsonFilePath = Path.Combine(exeDirectory, filePath);

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented, // 美观地格式化输出
                // 如果需要，可以在此处添加自定义的 JsonConverter
            };

            try
            {
                string json = JsonConvert.SerializeObject(serverInfoNodeDict, settings);
                File.WriteAllText(jsonFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving scene data: " + ex.Message);
            }
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

