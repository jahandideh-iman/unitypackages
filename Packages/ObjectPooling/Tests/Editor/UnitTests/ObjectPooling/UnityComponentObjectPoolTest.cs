using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Arman.ObjectPooling.Tests
{
    public class FakePoolableComponent : MonoBehaviour, IPoolable
    {
        public int AcquiredCount;
        public int ReleasedCount;

        public void OnAcquired()
        {
            AcquiredCount++;
        }

        public void OnReleased()
        {
            ReleasedCount++;
        }
    }

    public class FakeComponentMonoBehaviourPool : MonoBehaviourObjectPool<FakePoolableComponent> { }

    public class UnityComponentObjectPoolTest
    {
        readonly List<Object> _created = new List<Object>();

        FakePoolableComponent _prefab = null!;
        Transform _container = null!;

        [SetUp]
        public void Setup()
        {
            _prefab = Track(new GameObject("Prefab")).AddComponent<FakePoolableComponent>();
            _container = Track(new GameObject("Container")).transform;
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var obj in _created)
                if (obj != null)
                    Object.DestroyImmediate(obj);

            _created.Clear();
        }

        [Test]
        public void AcquiringShouldInstantiateANewCopyOfThePrefabUnderTheContainer()
        {
            var pool = CreatePool();

            var obj = pool.Acquire();

            Assert.That(obj, Is.Not.SameAs(_prefab));
            Assert.That(obj.transform.parent, Is.SameAs(_container));
            Assert.That(obj.AcquiredCount, Is.EqualTo(1));
        }

        [Test]
        public void ReleasingShouldReturnTheObjectUnderTheContainer()
        {
            var pool = CreatePool();
            var elsewhere = Track(new GameObject("Elsewhere")).transform;
            var obj = pool.Acquire();
            obj.transform.SetParent(elsewhere, false);

            pool.Release(obj);

            Assert.That(obj.transform.parent, Is.SameAs(_container));
            Assert.That(obj.ReleasedCount, Is.EqualTo(1));
            Assert.That(pool.Size(), Is.EqualTo(1));
        }

        [Test]
        public void ReservingShouldInstantiateTheObjectsUnderTheContainer()
        {
            var pool = CreatePool();

            pool.Reserve(3);

            Assert.That(pool.Size(), Is.EqualTo(3));
            Assert.That(_container.childCount, Is.EqualTo(3));
        }

        [Test]
        public void AcquiringAReleasedObjectShouldNotInstantiateAnotherCopy()
        {
            var pool = CreatePool();
            var obj = pool.Acquire();
            pool.Release(obj);

            var reacquired = pool.Acquire();

            Assert.That(reacquired, Is.SameAs(obj));
            Assert.That(_container.childCount, Is.EqualTo(1));
        }

        [Test]
        public void SettingUpAScriptableObjectPoolShouldReserveTheInitialAmountUnderTheContainer()
        {
            var pool = Track(ScriptableObject.CreateInstance<FakeComponentScriptablePool>());
            SetField(pool, "ComponentPrefab", _prefab);
            SetField(pool, "InitialReserve", 2);

            pool.Setup(_container);

            Assert.That(pool.Size(), Is.EqualTo(2));
            Assert.That(_container.childCount, Is.EqualTo(2));
        }

        [Test]
        public void AScriptableObjectPoolShouldAcquireAndReleaseThroughItsInternalPool()
        {
            var pool = Track(ScriptableObject.CreateInstance<FakeComponentScriptablePool>());
            SetField(pool, "ComponentPrefab", _prefab);
            pool.Setup(_container);

            var obj = pool.Acquire();
            Assert.That(pool.Size(), Is.EqualTo(0));

            pool.Release(obj);
            Assert.That(pool.Size(), Is.EqualTo(1));

            pool.Reserve(2);
            Assert.That(pool.Size(), Is.EqualTo(3));
        }

        [Test]
        public void SettingUpAMonoBehaviourPoolShouldReserveTheInitialAmountUnderItsContainer()
        {
            var pool = Track(new GameObject("Pool")).AddComponent<FakeComponentMonoBehaviourPool>();
            SetField(pool, "ComponentPrefab", _prefab);
            SetField(pool, "PoolingContainer", _container);
            SetField(pool, "InitialReserve", 2);

            pool.Setup();

            Assert.That(pool.Size(), Is.EqualTo(2));
            Assert.That(_container.childCount, Is.EqualTo(2));
        }

        [Test]
        public void AMonoBehaviourPoolShouldAcquireAndReleaseThroughItsInternalPool()
        {
            var pool = Track(new GameObject("Pool")).AddComponent<FakeComponentMonoBehaviourPool>();
            SetField(pool, "ComponentPrefab", _prefab);
            SetField(pool, "PoolingContainer", _container);
            pool.Setup();

            var obj = pool.Acquire();
            Assert.That(obj.transform.parent, Is.SameAs(_container));
            Assert.That(pool.Size(), Is.EqualTo(0));

            pool.Release(obj);
            Assert.That(pool.Size(), Is.EqualTo(1));
        }

        UnityComponentObjectPool<FakePoolableComponent> CreatePool()
        {
            var pool = new UnityComponentObjectPool<FakePoolableComponent>();
            pool.SetComponentPrefab(_prefab);
            pool.SetPoolingContainer(_container);
            return pool;
        }

        T Track<T>(T obj)
            where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        static void SetField(object target, string name, object value)
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo? field = null;
            for (var type = target.GetType(); field == null && type != null; type = type.BaseType)
                field = type.GetField(name, flags);
            Assert.That(field, Is.Not.Null, $"{target.GetType().Name} has no field '{name}'");
            field!.SetValue(target, value);
        }
    }
}
