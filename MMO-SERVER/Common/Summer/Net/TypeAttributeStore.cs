using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Summer.Net
{
    /// <summary>
    /// 一个通用的属性存储（自定义的）
    /// 缺点：同一个类型只能存储一个
    /// </summary>
    public class TypeAttributeStore
    {
        private Dictionary<string, object> _dict = new Dictionary<string, object>();

        /// <summary>
        /// 根据类型存放属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void Set<T>(T value)
        {
            string key = typeof(T).FullName;
            if (!_dict.ContainsKey(key))
            {
                _dict.Add(key, value);
            }
            else
            {
                _dict[key] = value;
            }
        }

        /// <summary>
        /// 根据类型获取属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            string key = typeof(T).FullName;
            if (_dict.ContainsKey(key))
            {
                return (T)_dict[key];
            }
            return default;
        }

    }
}
