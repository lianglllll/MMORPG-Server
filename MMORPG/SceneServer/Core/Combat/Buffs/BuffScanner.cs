using SceneServer.Core.Model.Actor;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SceneServer.Core.Combat.Buffs
{

    // 描述了一个只能应用在类上的特性
    [AttributeUsage(AttributeTargets.Class)]
    public class BuffAttribute : Attribute
    {
        public int BuffId { get; }

        public BuffAttribute(int buffId)
        {
            this.BuffId = buffId;
        }
    }


    public class BuffScanner
    {
        public static ConcurrentDictionary<int, Type> BuffTypeDict = new();

        // 扫描项目里带有[Skill]属性的class
        public static void Start()
        {
            int count = 0;
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            Type buffType = typeof(BuffBase);
            foreach (Type type in types)
            {
                // 判断这个type身上是否有BuffAttribute特性
                if (Attribute.IsDefined(type, typeof(BuffAttribute)))
                {
                    var attribute = (BuffAttribute)Attribute.GetCustomAttribute(type, typeof(BuffAttribute));
                    // 拿到我们在属性中存放的技能id
                    int buffId = attribute.BuffId;

                    // 判断当前类型是否为buffType类或者派生类
                    if (buffType.IsAssignableFrom(type.BaseType))
                    {
                        count++;
                        BuffTypeDict[buffId] = type;
                    }
                    else
                    {
                        Log.Error("未继承BuffBase基类:Name=[{1}]", buffId, type.Name);
                    }

                }
            }

            //Log.Debug("==>共加载{0}个自定义技能", count);
        }

        // 创建Skill实例
        public static BuffBase CreateBuff(int buffId)
        {
            // 1.如果有注解则使用所在的类型
            if (BuffTypeDict.TryGetValue(buffId, out var buffType))
            {
                object instance = Activator.CreateInstance(buffType);
                return (BuffBase)instance;
            }
            // 2.如果匹配不到则使用基础类型
            return null;
        }
    }
}
