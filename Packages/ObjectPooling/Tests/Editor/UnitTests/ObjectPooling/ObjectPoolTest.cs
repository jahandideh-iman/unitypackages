using Arman.ObjectPooling;
using NUnit.Framework;

namespace Arman.ObjectPooling.Tests
{
    public class FakePoolable : IPoolable
    {
        public int Id;

        public bool IsActive = false;

        public bool OnAcquiredIsCalled = false;
        public bool OnReleaseIsCalled = false;

        public FakePoolable(int id)
        {
            this.Id = id;
        }

        public void OnAcquired()
        {
            OnAcquiredIsCalled = true;
        }

        public void OnReleased()
        {
            OnReleaseIsCalled = true;
        }

        public void SetActive(bool isActive)
        {
            this.IsActive = isActive;
        }
    }

    public class TestableObjectPool : ObjectPool<FakePoolable>
    {
        public bool CreateMethodIsCalled = false;

        protected override FakePoolable CreateObject()
        {
            this.CreateMethodIsCalled = true;
            return new FakePoolable(1);
        }

        protected override void DeactivateObject(FakePoolable obj)
        {
            obj.SetActive(false);
        }

        protected override void ActivateObject(FakePoolable obj)
        {
            obj.SetActive(true);
        }
    }

    public class ObjectPoolTest
    {
        TestableObjectPool _pool;

        [SetUp]
        public void Setup()
        {
            _pool = new TestableObjectPool();
        }

        [Test]
        public void ANewPoolShouldBeEmpty()
        {
            Assert.That(_pool.Size(), Is.EqualTo(0));
        }

        [Test]
        public void AcquiringShouldCreateAObjectAndActivateItWhenNoObjectIsInThePool()
        {
            var obj = _pool.Acquire();

            Assert.That(_pool.CreateMethodIsCalled);
            Assert.That(obj.IsActive, Is.True);
            Assert.That(obj.OnAcquiredIsCalled, Is.True);
        }

        [Test]
        public void ReleasingAnObjectShouldDeactivateTheObjectAndReturnItToThePool()
        {
            var obj = _pool.Acquire();

            _pool.Release(obj);

            Assert.That(obj.IsActive, Is.False);
            Assert.That(obj.OnReleaseIsCalled, Is.True);
            Assert.That(_pool.Size(), Is.EqualTo(1));
        }

        [Test]
        public void AcquiringShouldNotCreateANewObjectWhenWhenThereAreObjectInThePool()
        {
            var firstObject = _pool.Acquire();
            _pool.Release(firstObject);

            var secondObject = _pool.Acquire();

            Assert.That(firstObject, Is.SameAs(secondObject));
        }

        [Test]
        public void ReservingShouldAddToTheSizeOfThePool()
        {
            _pool.Reserve(3);
            _pool.Reserve(2);

            Assert.That(_pool.Size(), Is.EqualTo(5));
        }

        [Test]
        public void AcquiringShouldDecreaseTheSizeOfThePool()
        {
            _pool.Reserve(3);

            _pool.Acquire();

            Assert.That(_pool.Size(), Is.EqualTo(2));
        }
    }
}
