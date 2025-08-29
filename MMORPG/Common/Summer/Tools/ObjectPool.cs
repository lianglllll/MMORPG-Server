using System;
using System.Collections.Concurrent;

namespace Common.Summer.Tools
{

    /// <summary>
    /// 对象池，比如说我们技能产生的投射物可以重复利用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T>
    {
        private readonly Func<T> _objectFactory;
        private readonly ConcurrentBag<T> _objects;
        private readonly int _maxSize; 

        public ObjectPool(Func<T> objectFactory, int maxSize = 50000)
        {
            _objectFactory = objectFactory;
            _objects = new ConcurrentBag<T>();
            _maxSize = maxSize;
        }

        public T GetObject()
        {
            return _objects.TryTake(out var obj) ? obj : _objectFactory();
        }

        public void ReturnObject(T obj)
        {
            if (_objects.Count < _maxSize)
            {
                _objects.Add(obj);
            }
            else
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

    }

}

