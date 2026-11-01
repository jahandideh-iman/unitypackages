using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.ObjectPooling
{
    public class ScriptableObjectPool<T> : ScriptableObject, IObjectPool<T>
        where T : Component, IPoolable
    {
        [FormerlySerializedAs("componentPrefab")]
        [SerializeField]
        T ComponentPrefab = default;

        [FormerlySerializedAs("initialReserve")]
        [SerializeField]
        int InitialReserve = default;

        protected UnityComponentObjectPool<T> InternalPool = new UnityComponentObjectPool<T>();

        public void Setup(Transform poolingContainer)
        {
            InternalPool.SetComponentPrefab(ComponentPrefab);
            InternalPool.SetPoolingContainer(poolingContainer);

            InternalPool.Reserve(InitialReserve);
        }

        public T Acquire()
        {
            return InternalPool.Acquire();
        }

        public void Release(T obj)
        {
            InternalPool.Release(obj);
        }

        public void Reserve(int count)
        {
            InternalPool.Reserve(count);
        }

        public int Size()
        {
            return InternalPool.Size();
        }
    }
}
