using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.ObjectPooling
{
    public class MonoBehaviourObjectPool<T> : MonoBehaviour, IObjectPool<T>
        where T : Component, IPoolable
    {
        [FormerlySerializedAs("componentPrefab")]
        [SerializeField]
        T ComponentPrefab = default;

        [FormerlySerializedAs("poolingContainer")]
        [SerializeField]
        Transform PoolingContainer = default;

        [FormerlySerializedAs("initialReserve")]
        [SerializeField]
        int InitialReserve = default;

        [FormerlySerializedAs("autoSetup")]
        [SerializeField]
        bool AutoSetup = default;

        protected UnityComponentObjectPool<T> InternalPool = new UnityComponentObjectPool<T>();

        void Awake()
        {
            if (AutoSetup)
                Setup();
        }

        public void Setup()
        {
            InternalPool.SetComponentPrefab(ComponentPrefab);
            InternalPool.SetPoolingContainer(PoolingContainer);

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
