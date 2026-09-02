namespace Arman.ComponentSystem
{
    public interface ICache
    {
        void TryCache(IComponent component);
    }

    public class CacheableBasicEntity<T> : BasicEntity where T : ICache
    {
        readonly T cache;

        public CacheableBasicEntity(T cache)
        {
            this.cache = cache;
        }

        protected override void OnComponentAdded(IComponent component)
        {
            cache.TryCache(component);
        }

        public T Cache()
        {
            return cache;
        }
    }

}