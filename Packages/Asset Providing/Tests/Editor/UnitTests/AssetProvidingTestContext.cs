using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arman.AssetProviding.Tests
{
    // Creates the assets a test hands to its providers, and destroys them after the test.
    public class AssetProvidingTestContext
    {
        readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void Teardown()
        {
            foreach (var obj in _created)
                if (obj != null)
                    Object.DestroyImmediate(obj);

            _created.Clear();
        }

        protected FakeScriptableAsset CreateScriptableAsset(string name)
        {
            var asset = Track(ScriptableObject.CreateInstance<FakeScriptableAsset>());
            asset.name = name;
            return asset;
        }

        protected TextAsset CreateTextAsset(string name)
        {
            var asset = Track(new TextAsset(name));
            asset.name = name;
            return asset;
        }

        protected GameObject CreateGameObject(string name)
        {
            return Track(new GameObject(name));
        }

        protected static TableBasedAssetProvider TableOf(params (string id, Object asset)[] entries)
        {
            var table = new Dictionary<string, Object>();
            foreach (var (id, asset) in entries)
                table.Add(id, asset);
            return new TableBasedAssetProvider(table);
        }

        protected T Track<T>(T obj)
            where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}
