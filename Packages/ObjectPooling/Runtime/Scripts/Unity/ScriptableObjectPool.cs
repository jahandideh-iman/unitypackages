
using Arman.ObjectPooling.Core;
using UnityEngine;

namespace Arman.ObjectPooling.Unity
{
    public class ScriptableObjectPool<T> : ScriptableObject, ObjectPool<T> where T : Component, Poolable
    {
        [SerializeField] T componentPrefab = default;
        [SerializeField] int initialReserve = default;

        protected UnityComponentObjectPool<T> internalPool = new UnityComponentObjectPool<T>();


        public void Setup(Transform poolingContainer)
        {
            internalPool.SetComponentPrefab(componentPrefab);
            internalPool.SetPoolingContainer(poolingContainer);

            internalPool.Reserve(initialReserve);
        }


        public T Acquire()
        {
            return internalPool.Acquire();
        }

        public void Release(T obj)
        {
            internalPool.Release(obj);
        }

        public void Reserve(int count)
        {
            internalPool.Reserve(count);
        }

        public int Size()
        {
            return internalPool.Size();
        }
    }
}