using NUnit.Framework;
using UnityEngine;

namespace Arman.AssetProviding.Tests
{
    public class TableBasedAssetProviderTest : AssetProvidingTestContext
    {
        [Test]
        public void LoadingByIdShouldReturnTheAssetStoredUnderThatId()
        {
            var first = CreateScriptableAsset("First");
            var second = CreateScriptableAsset("Second");
            var provider = TableOf(("first", first), ("second", second));

            Assert.That(provider.LoadAssetById<FakeScriptableAsset>("first"), Is.SameAs(first));
            Assert.That(provider.LoadAssetById<FakeScriptableAsset>("second"), Is.SameAs(second));
        }

        [Test]
        public void LoadingByIdShouldReturnTheComponentOfAStoredGameObject()
        {
            var gameObject = CreateGameObject("Prefab");
            var provider = TableOf(("prefab", gameObject));

            Assert.That(
                provider.LoadAssetById<Transform>("prefab"),
                Is.SameAs(gameObject.transform)
            );
        }

        [Test]
        public void LoadingByIdShouldReturnNullWhenTheAssetIsOfAnotherType()
        {
            var provider = TableOf(("text", CreateTextAsset("Text")));

            Assert.That(provider.LoadAssetById<FakeScriptableAsset>("text") == null, Is.True);
        }

        [Test]
        public void LoadingByIdShouldReturnNullWhenTheGameObjectLacksTheComponent()
        {
            var provider = TableOf(("prefab", CreateGameObject("Prefab")));

            Assert.That(provider.LoadAssetById<Camera>("prefab") == null, Is.True);
        }

        [Test]
        public void LoadingByTypeShouldReturnTheAssetOfThatType()
        {
            var scriptable = CreateScriptableAsset("Scriptable");
            var text = CreateTextAsset("Text");
            var provider = TableOf(("scriptable", scriptable), ("text", text));

            Assert.That(provider.LoadAssetByType<TextAsset>(), Is.SameAs(text));
            Assert.That(provider.LoadAssetByType<FakeScriptableAsset>(), Is.SameAs(scriptable));
        }

        [Test]
        public void LoadingByTypeShouldFindAComponentOnAStoredGameObject()
        {
            var gameObject = CreateGameObject("Prefab");
            var camera = gameObject.AddComponent<Camera>();
            var provider = TableOf(("text", CreateTextAsset("Text")), ("prefab", gameObject));

            Assert.That(provider.LoadAssetByType<Camera>(), Is.SameAs(camera));
        }

        [Test]
        public void LoadingByTypeShouldReturnNullWhenNoAssetHasThatType()
        {
            var provider = TableOf(
                ("text", CreateTextAsset("Text")),
                ("prefab", CreateGameObject("Prefab"))
            );

            Assert.That(provider.LoadAssetByType<FakeScriptableAsset>() == null, Is.True);
            Assert.That(provider.LoadAssetByType<Camera>() == null, Is.True);
        }

        [Test]
        public void LoadingAsynchronouslyShouldReturnWhatLoadingSynchronouslyReturns()
        {
            var scriptable = CreateScriptableAsset("Scriptable");
            var provider = TableOf(("scriptable", scriptable));

            var byId = provider.LoadAssetByIdAsync<FakeScriptableAsset>("scriptable");
            var byType = provider.LoadAssetByTypeAsync<FakeScriptableAsset>();

            Assert.That(byId.IsCompleted, Is.True);
            Assert.That(byId.Result, Is.SameAs(scriptable));
            Assert.That(byType.IsCompleted, Is.True);
            Assert.That(byType.Result, Is.SameAs(scriptable));
        }
    }
}
