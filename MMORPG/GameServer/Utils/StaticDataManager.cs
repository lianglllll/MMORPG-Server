using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Common.Summer.Tools;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;

namespace GameServer.Utils
{
    /// <summary>
    /// 用于导入json文件的数据
    /// </summary>
    public class StaticDataManager : Singleton<StaticDataManager>
    {
        //场景数据
        public Dictionary<int, UnitDefine> unitDefineDict = null;
        public Dictionary<int, ItemDefine> ItemDefinedDict = null;
        public Dictionary<int, TaskDefine> TaskDefinedDict = null;
        private Dictionary<int, Dictionary<int, TaskDefine>> TaskDefinedDict2 = null;

        //初始化，就是将文件中的数据读入
        public override void Init()
        {
            //获取文件对象信息
            unitDefineDict = Load<UnitDefine>("UnitDefine.json");
            ItemDefinedDict = Load<ItemDefine>("ItemDefine.json");
            TaskDefinedDict = Load<TaskDefine>("TaskDefine.json");
            HandleTasks();
        }

        //根据path加载解析json文件转换为dict
        private Dictionary<int, T> Load<T>(string filePath)
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
        private void HandleTasks()
        {
            TaskDefinedDict2 = new();
            foreach(var taskDef in TaskDefinedDict.Values)
            {
                int chainId = taskDef.Chain_id;
                int subId = taskDef.Sub_id;
                if (!TaskDefinedDict2.TryGetValue(chainId, out var tasks))
                {
                    tasks = new Dictionary<int, TaskDefine>();
                    TaskDefinedDict2.Add(chainId, tasks);
                }
                tasks.Add(subId, taskDef);
            }
        }
        public TaskDefine GetChainTaskDefine(int chainId, int subId)
        {
            TaskDefine result = null;
            TaskDefinedDict2.TryGetValue(chainId, out var tasks);
            if (tasks == null)
            {
                goto End;
            }
            tasks.TryGetValue(subId, out result);
        End:
            return result;
        }
        public TaskDefine GetTaskDefineByTaskId(int taskId)
        {
            TaskDefinedDict.TryGetValue(taskId, out var taskDef);
            return taskDef;
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

