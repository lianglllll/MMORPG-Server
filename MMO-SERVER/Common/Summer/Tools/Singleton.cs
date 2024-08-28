using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
	//泛型弄出了很多个不同的Singleton类，所以多个子类来继承这个Singleton的时候并不会发生使用同一个static属性的问题
	public class Singleton<T> where T : new()
	{
		private static T instance;
		private static object lockObj = new object();
		public static T Instance
		{
			get
			{
				if (instance != null) return instance;
                lock (lockObj)
                {
					if(instance == null)		//防止极端情况
                    {
						instance = new T();
                    }
                }
				return instance;
			}
		}
	}
}
