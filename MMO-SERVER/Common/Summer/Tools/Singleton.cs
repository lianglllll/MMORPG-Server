namespace Common.Summer.Tools
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
        
		// 防止外部实例化
        protected Singleton() { }

        // 可选初始化方法
        public virtual void Init() { }
    }
}
