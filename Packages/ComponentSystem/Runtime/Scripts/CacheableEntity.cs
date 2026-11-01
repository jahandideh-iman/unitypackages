namespace Arman.ComponentSystem
{
    public interface ICache
    {
        void TryCache(IComponent component);
    }

    public class CacheableEntity<T> : Entity
        where T : ICache
    {
        readonly T _cache;

        public CacheableEntity(T cache)
        {
            this._cache = cache;
        }

        protected override void OnComponentAdded(IComponent component)
        {
            _cache.TryCache(component);
        }

        public T Cache()
        {
            return _cache;
        }
    }
}
