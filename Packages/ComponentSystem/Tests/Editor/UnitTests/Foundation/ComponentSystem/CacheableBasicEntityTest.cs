using Arman.Foundation.ComponentSystem.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.ComponentSystem.Core
{
    public class CacheableBasicEntityTest
    {
        class CacheMock : Cache
        {
            public List<Component> components = new List<Component>();

            public void TryCache(Component component)
            {
                components.Add(component);
            }
        }

        [Test]
        public void AddingComponentShouldCallTryCache()
        {
            var entity = new CacheableBasicEntity<CacheMock>(new CacheMock());

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