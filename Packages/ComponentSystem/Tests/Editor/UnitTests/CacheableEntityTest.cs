using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.ComponentSystem.Tests
{
    public class CacheableEntityTest
    {
        class CacheMock : ICache
        {
            public List<IComponent> components = new List<IComponent>();

            public void TryCache(IComponent component)
            {
                components.Add(component);
            }
        }

        [Test]
        public void AddingComponentShouldCallTryCache()
        {
            var entity = new CacheableEntity<CacheMock>(new CacheMock());

            entity.AddComponents(
                new ComponentA(),
                new ComponentB(),
                new ComponentC());

            Assert.That(entity.Cache().components[0], Is.TypeOf<ComponentA>());
            Assert.That(entity.Cache().components[1], Is.TypeOf<ComponentB>());
            Assert.That(entity.Cache().components[2], Is.TypeOf<ComponentC>());
        }
    }
}