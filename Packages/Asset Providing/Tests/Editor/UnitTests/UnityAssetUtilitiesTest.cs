using NUnit.Framework;
using UnityEngine;

namespace Arman.AssetProviding.Tests
{
    public class UnityAssetUtilitiesTest : AssetProvidingTestContext
    {
        [Test]
        public void AnAssetShouldBeOfItsOwnType()
        {
            var asset = CreateScriptableAsset("Asset");

            Assert.That(
                UnityAssetUtilities.IsOfAssetType<FakeScriptableAsset>(asset, out var casted),
                Is.True
            );
            Assert.That(casted, Is.SameAs(asset));
        }

        [Test]
        public void AnAssetShouldNotBeOfAnUnrelatedType()
        {
            var asset = CreateTextAsset("Text");

            Assert.That(
                UnityAssetUtilities.IsOfAssetType<FakeScriptableAsset>(asset, out var casted),
                Is.False
            );
            Assert.That(casted == null, Is.True);
        }

        [Test]
        public void AGameObjectShouldBeOfTheTypeOfAComponentItCarries()
        {
            var gameObject = CreateGameObject("Prefab");
            var camera = gameObject.AddComponent<Camera>();

            Assert.That(
                UnityAssetUtilities.IsOfAssetType<Camera>(gameObject, out var casted),
                Is.True
            );
            Assert.That(casted, Is.SameAs(camera));
        }

        [Test]
        public void AGameObjectShouldNotBeOfTheTypeOfAComponentItLacks()
        {
            var gameObject = CreateGameObject("Prefab");

            Assert.That(
                UnityAssetUtilities.IsOfAssetType<Camera>(gameObject, out var casted),
                Is.False
            );
            Assert.That(casted == null, Is.True);
        }

        [Test]
        public void CastingAnAssetShouldReturnItAsTheRequestedType()
        {
            var asset = CreateScriptableAsset("Asset");

            Assert.That(UnityAssetUtilities.CastToAsset<ScriptableObject>(asset), Is.SameAs(asset));
            Assert.That(UnityAssetUtilities.CastToAsset<TextAsset>(asset) == null, Is.True);
        }

        [Test]
        public void CastingAGameObjectShouldReturnTheRequestedComponent()
        {
            var gameObject = CreateGameObject("Prefab");

            Assert.That(
                UnityAssetUtilities.CastToAsset<Transform>(gameObject),
                Is.SameAs(gameObject.transform)
            );
            Assert.That(UnityAssetUtilities.CastToAsset<Camera>(gameObject) == null, Is.True);
        }
    }
}
