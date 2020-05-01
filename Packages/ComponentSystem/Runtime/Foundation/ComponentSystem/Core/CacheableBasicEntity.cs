namespace Arman.Foundation.ComponentSystem.Core
{
    public interface Cache
    {
        void TryCache(Component component);
    }

    public class CacheableBasicEntity<T> : BasicEntity where T : Cache
    {
        readonly T cache;

        public CacheableBasicEntity(T cache)
        {
            this.cache = cache;
        }

        protected override void OnComponentAdded(Component component)
        {
            cache.TryCache(component);
        }

        public T Cache()
        {
            return cache;
        }
    }

}