using SceneServer.Core.Model.Actor;
using Serilog;
using System.Collections.Concurrent;
using System.Reflection;


namespace SceneServer.Core.Combat.Skills
{
    //描述了一个只能应用在类上的特性
    [AttributeUsage(AttributeTargets.Class)]
    public class SkillAttribute : Attribute
    {
        //技能码  其实就是skillid
        public int Code { get; }

        public SkillAttribute(int code)
        {
            this.Code = code;
        }
    }

    public class SkillSanner
    {
        public static ConcurrentDictionary<int, Type> SkillTypeDict = new();

        // 扫描项目里带有[Skill]属性的class
        public static void Start()
        {
            int count = 0;
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            Type skillType = typeof(Skill);
            foreach (Type type in types)
            {
                //判断这个type身上是否有SkillAttribute特性
                if (Attribute.IsDefined(type, typeof(SkillAttribute)))
                {
                    var attribute = (SkillAttribute)Attribute.GetCustomAttribute(type, typeof(SkillAttribute));
                    //拿到我们在属性中存放的技能id
                    int skid = attribute.Code;

                    //判断当前类型是否为skillType类或者派生类
                    if (skillType.IsAssignableFrom(type.BaseType))
                    {
                        count++;
                        SkillTypeDict[skid] = type;
                        //Log.Information("加载技能类型:Code=[{0}],Name=[{1}]", skid, type.Name);
                    }
                    else
                    {
                        Log.Error("未继承Skill基类:Name=[{1}]", skid, type.Name);
                    }
                    
                }
            }

            //Log.Debug("==>共加载{0}个自定义技能", count);
        }

        // 创建Skill实例
        public static Skill CreateSkill(SceneActor owner, int skid)
        {
            //1.如果有注解则使用所在的类型
            if (SkillTypeDict.TryGetValue(skid, out var skillType))
            {
                object instance = Activator.CreateInstance(skillType, owner, skid);
                return (Skill)instance;
            }
            //2.如果匹配不到则使用基础类型
            return new Skill(owner, skid);
        }
    }

}
