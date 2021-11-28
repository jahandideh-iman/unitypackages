
using Arman.ObjectPooling.Core;
using UnityEngine;

namespace Arman.ObjectPooling.Unity
{
    public class MonobehaviorObjectPool<T>: MonoBehaviour, ObjectPool<T> where T : Component, Poolable
    {
        [SerializeField] T componentPrefab = default;
        [SerializeField] Transform poolingContainer = default;
        [SerializeField] int initialReserve = default;
        [SerializeField] bool autoSetup = default;

        protected UnityComponentObjectPool<T> internalPool = new UnityComponentObjectPool<T>();

        void Awake()
        {
            if (autoSetup)
                Setup();
        }

        public void Setup()
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