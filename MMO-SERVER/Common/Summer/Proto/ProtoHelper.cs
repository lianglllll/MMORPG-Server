using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using System.IO;
using System;
using System.Reflection;
using System.Linq;
using Google.Protobuf.Reflection;
using Serilog;

namespace GameServer
{
    /// <summary>
    /// Protobuf序列化与反序列化
    /// </summary>
    public class ProtoHelper
    {

        //字典用于保存message的所有类型,用于拆包时进行类型转换
        //<fullName,Type>
        private static Dictionary<string, Type> _registry = new Dictionary<string, Type>();

        //考虑到每次输送类型名太长了，所以当扫描完imessage类型后给每一个类型一个序号
        private static Dictionary<int, Type> mDict1 = new Dictionary<int, Type>();
        private static Dictionary<Type, int> mDict2 = new Dictionary<Type, int>();

        /// <summary>
        /// 根据类型获取协议在中网络传输的id值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int SeqCode(Type type)
        {
            return mDict2[type];
        }

        /// <summary>
        /// 根据协议在中网络传输的id值获取协议的类型
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Type SeqType(int code)
        {
            return mDict1[code];
        }

        /// <summary>
        /// 用静态代码块加载注册
        /// </summary>
        static ProtoHelper()
        {
            List<string> list = new List<string>();
            //  LINQ 查询语法，获取当前正在执行的程序集中的所有类型。
            //var q = from t in Assembly.GetExecutingAssembly().GetTypes() select t;
            var q = Assembly.GetExecutingAssembly().GetTypes();

            q.ToList().ForEach(t =>
            {
                if (typeof(IMessage).IsAssignableFrom(t))
                {
                    //t现在是imessage的子类
                   var desc =  t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;
                    _registry.Add(desc.FullName,t);
                    list.Add(desc.FullName);
                }
            });

            //根据协议名的字符串进行排序
            list.Sort((x, y) =>
            {
                //根据字符串长度排序
                if (x.Length != y.Length)
                {
                    return x.Length - y.Length;
                }
                //如果长度相同
                //则使用x和y基于 Unicode码点值的排序规则进行字符串比较，保证了排序的稳定性(大白话就算对应的整型值，x<y就返回负数)
                return string.Compare(x, y, StringComparison.Ordinal);
            });

            for (int i = 0; i < list.Count; i++)
            {
                var fname = list[i];
                //Log.Debug("Proto类型注册：{0}  {1}", i,fname);
                var t = _registry[fname];
                mDict1.Add(i, t);
                mDict2.Add(t, i);
            }
            Log.Debug("==>共加载{0}个proto协议", list.Count);

        }

        /// <summary>
        /// 根据协议在中网络传输的id值解析成一个imassage
        /// </summary>
        /// <param name="typeCode"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static IMessage ParseFrom(int typeCode,byte[] data ,int offset,int len)
        {
            Type t = ProtoHelper.SeqType(typeCode);
            var desc = t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;
            var msg = desc.Parser.ParseFrom(data, 2, data.Length - 2);
            return msg;
        }

        //初始化
        public static void Init()
        {

        }


        #region 弃用

         /*/// <summary>
        /// 序列化protobuf
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] Serialize(IMessage msg)
        {
            using (MemoryStream rawOutput = new MemoryStream())
            {
                msg.WriteTo(rawOutput);
                byte[] result = rawOutput.ToArray();
                return result;
            }
        }
        /// <summary>
        /// 解析
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        public static T Parse<T>(byte[] dataBytes) where T : IMessage, new()
        {
            T msg = new T();
            msg = (T)msg.Descriptor.Parser.ParseFrom(dataBytes);
            return msg;
        }*/

        /*        /// <summary>
                /// 打包message->package
                /// </summary>
                /// <param name="message"></param>
                /// <returns></returns>
                public static Proto.Package Pack(IMessage message)
                {
                    Proto.Package package = new Proto.Package();
                    package.Fullname = message.Descriptor.FullName;//全限定类名
                    package.Data = message.ToByteString();
                    return package;
                }

                /// <summary>
                /// 拆包
                /// </summary>
                /// <param name="package"></param>
                /// <returns></returns>
                public static IMessage Unpack(Proto.Package package) 
                {
                    //这里一个难点就是如何获取fullname这个类型
                    string fullName = package.Fullname;
                    if (_registry.ContainsKey(fullName))
                    {
                        Type t = _registry[fullName];
                        var desc = t.GetProperty("Descriptor").GetValue(t) as MessageDescriptor;
                        return desc.Parser.ParseFrom(package.Data);
                    }
                    return null;
                }


        */
        #endregion
    }
}
