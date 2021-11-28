using Arman.ObjectPooling.Core;
using NUnit.Framework;

namespace Arman.Tests.ObjectPooling.Core
{

    public class MockObject : Poolable
    {
        public int id;

        public bool isActive = false;

        public bool onAcquiredIsCalled = false;
        public bool onReleaseIsCalled = false;

        public MockObject(int id)
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

    public class TestableBasicObjectPool : BasicObjectPool<MockObject>
    {

        public bool createMethodIsCalled = false;


        protected override MockObject CreateObject()
        {
            this.createMethodIsCalled = true;
            return new MockObject(1);
        }

        protected override void DeactivateObject(MockObject obj)
        {
            obj.SetActive(false);
        }


        protected override void ActivateObject(MockObject obj)
        {
            obj.SetActive(true);
        }
    }


    public class BasicObjectPoolTest 
    {
        TestableBasicObjectPool pool;


        [SetUp]
        public void Setup()
        {
            pool = new TestableBasicObjectPool();
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