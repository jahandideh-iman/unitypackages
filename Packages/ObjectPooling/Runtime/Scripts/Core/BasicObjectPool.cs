
using System.Collections.Generic;

namespace Arman.ObjectPooling.Core
{
    public abstract class BasicObjectPool<T> : ObjectPool<T> where T: Poolable
    {
        Stack<T> pooledObjects = new Stack<T>();

        public T Acquire()
        {
            T obj = default(T);

            if (pooledObjects.Count > 0)
                obj = RemoveAnObjectFromPool();
            else
                obj = CreateObject();

            ActivateObject(obj);
            obj.OnAcquired();

            return obj;
        }

        private T RemoveAnObjectFromPool()
        {
            return pooledObjects.Pop();
        }

        public void Release(T obj)
        {
            pooledObjects.Push(obj);
            DeactivateObject(obj);
            obj.OnReleased();
        }

        public void Reserve(int count)
        {
            for (int i = 0; i < count; i++)
                Release(CreateObject());
        }

        public int Size()
        {
            return pooledObjects.Count;
        }

        protected abstract T CreateObject();

        protected abstract void DeactivateObject(T obj);
        protected abstract void ActivateObject(T obj);

    }
}