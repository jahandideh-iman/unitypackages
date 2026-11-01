using NUnit.Framework;

namespace Arman.AssetProviding.Tests
{
    public class ChainedAssetProviderTest : AssetProvidingTestContext
    {
        [Test]
        public void LoadingByIdShouldFallThroughToTheNextProviderThatHasTheAsset()
        {
            var asset = CreateScriptableAsset("Asset");
            var chain = new ChainedSyncUnityAssetProvider();
            chain.Add(TableOf(("other", CreateScriptableAsset("Other"))));
            chain.Add(TableOf(("asset", asset)));

            Assert.That(chain.LoadAssetById<FakeScriptableAsset>("asset"), Is.SameAs(asset));
        }

        [Test]
        public void LoadingByIdShouldReturnTheAssetOfTheFirstProviderThatHasIt()
        {
            var first = CreateScriptableAsset("First");
            var chain = new ChainedSyncUnityAssetProvider();
            chain.Add(TableOf(("asset", first)));
            chain.Add(TableOf(("asset", CreateScriptableAsset("Second"))));

            Assert.That(chain.LoadAssetById<FakeScriptableAsset>("asset"), Is.SameAs(first));
        }

        [Test]
        public void LoadingByTypeShouldFallThroughToTheNextProviderThatHasTheType()
        {
            var text = CreateTextAsset("Text");
            var chain = new ChainedSyncUnityAssetProvider();
            chain.Add(TableOf(("scriptable", CreateScriptableAsset("Scriptable"))));
            chain.Add(TableOf(("text", text)));

            Assert.That(chain.LoadAssetByType<UnityEngine.TextAsset>(), Is.SameAs(text));
        }

        [Test]
        public void LoadingFromAChainWithoutTheAssetShouldReturnNull()
        {
            var chain = new ChainedSyncUnityAssetProvider();
            chain.Add(TableOf(("text", CreateTextAsset("Text"))));

            Assert.That(chain.LoadAssetByType<FakeScriptableAsset>() == null, Is.True);
        }

        [Test]
        public void LoadingFromAnEmptyChainShouldReturnNull()
        {
            var chain = new ChainedSyncUnityAssetProvider();

            Assert.That(chain.LoadAssetById<FakeScriptableAsset>("asset") == null, Is.True);
            Assert.That(chain.LoadAssetByType<FakeScriptableAsset>() == null, Is.True);
        }

        [Test]
        public void ARemovedProviderShouldNoLongerBeAskedForAssets()
        {
            var removed = TableOf(("asset", CreateScriptableAsset("Removed")));
            var kept = CreateScriptableAsset("Kept");
            var chain = new ChainedSyncUnityAssetProvider();
            chain.Add(removed);
            chain.Add(TableOf(("asset", kept)));

            chain.Remove(removed);

            Assert.That(chain.LoadAssetById<FakeScriptableAsset>("asset"), Is.SameAs(kept));
        }

        [Test]
        public void LoadingAsynchronouslyByIdShouldFallThroughToTheNextProviderThatHasTheAsset()
        {
            var asset = CreateScriptableAsset("Asset");
            var chain = new ChainedAsyncUnityAssetProvider();
            chain.Add(TableOf(("other", CreateScriptableAsset("Other"))));
            chain.Add(TableOf(("asset", asset)));

            var loading = chain.LoadAssetByIdAsync<FakeScriptableAsset>("asset");

            Assert.That(loading.IsCompleted, Is.True);
            Assert.That(loading.Result, Is.SameAs(asset));
        }

        [Test]
        public void LoadingAsynchronouslyByTypeShouldFallThroughToTheNextProviderThatHasTheType()
        {
            var text = CreateTextAsset("Text");
            var chain = new ChainedAsyncUnityAssetProvider();
            chain.Add(TableOf(("scriptable", CreateScriptableAsset("Scriptable"))));
            chain.Add(TableOf(("text", text)));

            var loading = chain.LoadAssetByTypeAsync<UnityEngine.TextAsset>();

            Assert.That(loading.IsCompleted, Is.True);
            Assert.That(loading.Result, Is.SameAs(text));
        }

        [Test]
        public void LoadingAsynchronouslyFromAnEmptyChainShouldReturnNull()
        {
            var chain = new ChainedAsyncUnityAssetProvider();

            Assert.That(
                chain.LoadAssetByIdAsync<FakeScriptableAsset>("asset").Result == null,
                Is.True
            );
            Assert.That(chain.LoadAssetByTypeAsync<FakeScriptableAsset>().Result == null, Is.True);
        }

        [Test]
        public void TheServiceShouldLoadThroughTheProvidersAddedToIt()
        {
            var syncAsset = CreateScriptableAsset("Sync");
            var asyncAsset = CreateScriptableAsset("Async");
            var service = new ChainedAssetProviderService();

            service.AddSyncProvider(TableOf(("sync", syncAsset)));
            service.AddAsyncProvider(TableOf(("async", asyncAsset)));

            Assert.That(
                service.ISyncUnityAssetProvider.LoadAssetById<FakeScriptableAsset>("sync"),
                Is.SameAs(syncAsset)
            );
            Assert.That(
                service
                    .IAsyncUnityAssetProvider.LoadAssetByIdAsync<FakeScriptableAsset>("async")
                    .Result,
                Is.SameAs(asyncAsset)
            );
        }
    }
}
