using UnityEngine;

namespace Arman.ObjectPooling
{
    public class UnityComponentObjectPool<T> : ObjectPool<T>
        where T : Component, IPoolable
    {
        protected Transform PoolingContainer;
        protected T ComponentPrefab;

        public void SetPoolingContainer(Transform transform)
        {
            this.PoolingContainer = transform;
        }

        public void SetComponentPrefab(T prefab)
        {
            this.ComponentPrefab = prefab;
        }

        protected override void ActivateObject(T obj) { }

        protected override void DeactivateObject(T obj)
        {
            obj.gameObject.transform.SetParent(PoolingContainer, false);
        }

        protected override T CreateObject()
        {
            var obj = UnityEngine.Object.Instantiate(ComponentPrefab, PoolingContainer, false);
            return obj;
        }
    }
}
