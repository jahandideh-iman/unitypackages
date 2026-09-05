namespace Arman.ComponentSystem
{
    public interface ICache
    {
        void TryCache(IComponent component);
    }

    public class CacheableEntity<T> : Entity where T : ICache
    {
        readonly T cache;

        public CacheableEntity(T cache)
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