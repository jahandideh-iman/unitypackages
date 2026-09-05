using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace Arman.ComponentSystem.Tests
{
    public class CacheableEntityTest
    {
        [Test]
        public void AddingComponentShouldCallTryCache()
        {
            var cached = new List<IComponent>();

            var cache = new Mock<ICache>();
            cache.Setup(c => c.TryCache(It.IsAny<IComponent>()))
                .Callback<IComponent>(cached.Add);

            var entity = new CacheableEntity<ICache>(cache.Object);

            entity.AddComponents(
                new ComponentA(),
                new ComponentB(),
                new ComponentC());

            Assert.That(cached[0], Is.TypeOf<ComponentA>());
            Assert.That(cached[1], Is.TypeOf<ComponentB>());
            Assert.That(cached[2], Is.TypeOf<ComponentC>());
        }
    }
}
