using Summer;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System;
using Newtonsoft.Json.Linq;

/// <summary>
/// 用于导入json文件的数据
/// </summary>
public class DataManager : Singleton<DataManager>
{

    //地图场景数据
    public Dictionary<int, SpaceDefine> spaceDefineDict = null;
    //单位类型数据
    public Dictionary<int, UnitDefine> unitDefineDict = null;
    //刷怪点的信息
    public Dictionary<int, SpawnDefine> spawnDefineDict = null;
    //技能信息
    public Dictionary<int, SkillDefine> skillDefineDict = null;
    //item信息
    public Dictionary<int, ItemDefine> ItemDefinedDict = null;
    //等级信息
    public Dictionary<int, LevelDefine> levelDefindeDict = null;
    //buffxinx
    public Dictionary<int,BuffDefine> buffDefindeDict = null;
    //一些复活点信息
    public Dictionary<int, RevivalPointDefine> revivalPointDefindeDict = null;


    //初始化，就是将文件中的数据读入
    public void init()
    {
        //获取文件对象信息
        spaceDefineDict = Load<SpaceDefine>("Data/SpaceDefine.json");
        unitDefineDict = Load<UnitDefine>("Data/UnitDefine.json");
        spawnDefineDict = Load<SpawnDefine>("Data/SpawnDefine.json");
        skillDefineDict = Load<SkillDefine>("Data/SkillDefine.json");
        ItemDefinedDict = Load<ItemDefine>("Data/ItemDefine.json");
        levelDefindeDict = Load<LevelDefine>("Data/LevelDefine.json");
        buffDefindeDict = Load<BuffDefine>("Data/BuffDefine.json");
        revivalPointDefindeDict = Load<RevivalPointDefine>("Data/RevivalPointDefine.json");
    }

    //根据path加载解析json文件转换为dict
    public Dictionary<int, T> Load<T>(string filePath)
    {
        //获取exe文件所在目录的绝对路径
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string exeDirectory = Path.GetDirectoryName(exePath);
        //构建.exe文件完整类路径
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

        return JsonConvert.DeserializeObject<Dictionary<int, T>>(context,settings);

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
