using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace Arman.ComponentSystem.Tests
{
    // A cache that keeps what it is offered, so a test can read back what reached it.
    class FakeCache : ICache
    {
        public readonly List<IComponent> Cached = new List<IComponent>();

        public void TryCache(IComponent component)
        {
            Cached.Add(component);
        }
    }

    public class CacheableEntityTest
    {
        [Test]
        public void AddingComponentShouldCallTryCache()
        {
            var cached = new List<IComponent>();

            var cache = new Mock<ICache>();
            cache.Setup(c => c.TryCache(It.IsAny<IComponent>())).Callback<IComponent>(cached.Add);

            var entity = new CacheableEntity<ICache>(cache.Object);

            entity.AddComponents(new ComponentA(), new ComponentB(), new ComponentC());

            Assert.That(cached[0], Is.TypeOf<ComponentA>());
            Assert.That(cached[1], Is.TypeOf<ComponentB>());
            Assert.That(cached[2], Is.TypeOf<ComponentC>());
        }

        [Test]
        public void AddingASingleComponentShouldOfferItToTheCache()
        {
            var cache = new FakeCache();
            var entity = new CacheableEntity<FakeCache>(cache);

            var component = new ComponentA();
            entity.AddComponent(component);

            Assert.That(cache.Cached, Is.EqualTo(new IComponent[] { component }));
        }

        [Test]
        public void CacheShouldReturnTheCacheTheEntityWasCreatedWith()
        {
            var cache = new FakeCache();

            var entity = new CacheableEntity<FakeCache>(cache);

            Assert.That(entity.Cache(), Is.SameAs(cache));
        }

        [Test]
        public void ComponentsOfferedToTheCacheShouldStillBeFindableOnTheEntity()
        {
            var entity = new CacheableEntity<FakeCache>(new FakeCache());

            var component = new ComponentB();
            entity.AddComponent(component);

            Assert.That(entity.GetComponent<ComponentB>(), Is.SameAs(component));
        }
    }
}
