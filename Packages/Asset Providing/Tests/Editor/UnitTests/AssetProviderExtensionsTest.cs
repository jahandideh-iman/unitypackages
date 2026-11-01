using NUnit.Framework;
using UnityEngine;

namespace Arman.AssetProviding.Tests
{
    public class AssetProviderExtensionsTest : AssetProvidingTestContext
    {
        GameObject _prefab = null!;
        Transform _parent = null!;
        TableBasedAssetProvider _provider = null!;

        [SetUp]
        public void Setup()
        {
            _prefab = CreateGameObject("Prefab");
            _prefab.AddComponent<Camera>();
            _parent = CreateGameObject("Parent").transform;
            _provider = TableOf(("prefab", _prefab));
        }

        [Test]
        public void InstantiatingByIdShouldCreateACopyUnderTheParent()
        {
            var instance = _provider.InstantiateById<Camera>("prefab", _parent);

            Assert.That(instance.gameObject, Is.Not.SameAs(_prefab));
            Assert.That(instance.transform.parent, Is.SameAs(_parent));
        }

        [Test]
        public void InstantiatingByTypeShouldCreateACopyUnderTheParent()
        {
            var instance = _provider.InstantiateByType<Camera>(_parent);

            Assert.That(instance.gameObject, Is.Not.SameAs(_prefab));
            Assert.That(instance.transform.parent, Is.SameAs(_parent));
        }

        [Test]
        public void InstantiatingAsynchronouslyShouldCreateACopyUnderTheParent()
        {
            var byId = _provider.InstantiateByIdAsync<Camera>("prefab", _parent);
            var byType = _provider.InstantiateByTypeAsync<Camera>(_parent);

            Assert.That(byId.IsCompleted, Is.True);
            Assert.That(byId.Result.transform.parent, Is.SameAs(_parent));
            Assert.That(byType.IsCompleted, Is.True);
            Assert.That(byType.Result.transform.parent, Is.SameAs(_parent));
            Assert.That(_parent.childCount, Is.EqualTo(2));
        }
    }
}
