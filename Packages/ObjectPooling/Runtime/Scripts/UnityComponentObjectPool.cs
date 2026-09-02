
using UnityEngine;

namespace Arman.ObjectPooling
{
    public class UnityComponentObjectPool<T> : BasicObjectPool<T> where T : Component, IPoolable 
    {
        protected Transform poolingContainer;
        protected T componentPrefab;

        public void SetPoolingContainer(Transform transform)
        {
            this.poolingContainer = transform;
        }

        public void SetComponentPrefab(T prefab)
        {
            this.componentPrefab = prefab;
        }

        protected override void ActivateObject(T obj)
        {
            
        }

        protected override void DeactivateObject(T obj)
        {
            obj.gameObject.transform.SetParent(poolingContainer, false);
        }

        protected override T CreateObject()
        {
            var obj = UnityEngine.Object.Instantiate(componentPrefab, poolingContainer, false);
            return obj;
        }

    }
}