using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    [TestFixture]
    public class PersistentDataManagerTest_Registering : PersistentDataManagerTestContext
    {
        [Test]
        public void HasTheRegisterSerializersWithoutChannel()
        {
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);

            Assert.That(manager.Contains(serializerA.Object));
            Assert.That(manager.Contains(serializerB.Object));
        }

        [Test]
        public void HasTheRegisterSerializersWithChannel()
        {
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            Assert.That(manager.Contains(serializerA.Object));
            Assert.That(manager.Contains(serializerB.Object));
        }

        [Test]
        public void RegisteringASerializerOnTwoChannelsShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Register(serializerA.Object, channel1);
                manager.Register(serializerA.Object, channel2);
            }
            );

            Assert.That(action, Throws.Exception.InstanceOf<PersistentDataSerializerAlreadyRegisterException>());

            // TODO: Shoud I not assert that serializerA is not registered in channelB?
        }
    }
}
