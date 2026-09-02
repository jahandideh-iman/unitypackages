
using UnityEngine;

namespace Arman.ObjectPooling
{
    public class ScriptableObjectPool<T> : ScriptableObject, IObjectPool<T> where T : Component, IPoolable
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