using Arman.ObjectPooling;
using NUnit.Framework;

namespace Arman.ObjectPooling.Tests
{

    public class FakePoolable : IPoolable
    {
        public int id;

        public bool isActive = false;

        public bool onAcquiredIsCalled = false;
        public bool onReleaseIsCalled = false;

        public FakePoolable(int id)
        {
            this.id = id;
        }

        public void OnAcquired()
        {
            onAcquiredIsCalled = true;
        }

        public void OnReleased()
        {
            onReleaseIsCalled = true;
        }

        public void SetActive(bool isActive)
        {
            this.isActive = isActive;
        }
    }

    public class TestableObjectPool : ObjectPool<FakePoolable>
    {

        public bool createMethodIsCalled = false;


        protected override FakePoolable CreateObject()
        {
            this.createMethodIsCalled = true;
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
        TestableObjectPool pool;


        [SetUp]
        public void Setup()
        {
            pool = new TestableObjectPool();
        }

        [Test]
        public void ANewPoolShouldBeEmpty()
        {
            Assert.That(pool.Size(), Is.EqualTo(0));
        }

        [Test]
        public void AcquiringShouldCreateAObjectAndActivateItWhenNoObjectIsInThePool()
        {
            var obj = pool.Acquire();

            Assert.That(pool.createMethodIsCalled);
            Assert.That(obj.isActive, Is.True);
            Assert.That(obj.onAcquiredIsCalled, Is.True);
        }

        [Test]
        public void ReleasingAnObjectShoudDeactiveTheObjectAndReturnItToThePool()
        {
            var obj = pool.Acquire();

            pool.Release(obj);

            Assert.That(obj.isActive, Is.False);
            Assert.That(obj.onReleaseIsCalled, Is.True);
            Assert.That(pool.Size(), Is.EqualTo(1));
        }


        [Test]
        public void AcquiringShouldNotCreateANewObjectWhenWhenThereAreObjectInThePool()
        {
            var firstObject = pool.Acquire();
            pool.Release(firstObject);

            var secondObject = pool.Acquire();

            Assert.That(firstObject, Is.SameAs(secondObject));

        }

        [Test]
        public void ReservingShoudAddToTheSizeOfThePool()
        {
            pool.Reserve(3);
            pool.Reserve(2);

            Assert.That(pool.Size(), Is.EqualTo(5));
        }

        [Test]
        public void AcquiringShoudDecreaseTheSizeOfThePool()
        {
            pool.Reserve(3);

            pool.Acquire();

            Assert.That(pool.Size(), Is.EqualTo(2));
        }

    }
}