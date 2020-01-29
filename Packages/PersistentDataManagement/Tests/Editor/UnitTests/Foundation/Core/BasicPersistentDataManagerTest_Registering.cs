using Arman.Foundation.Core.PersistentDataManagement;
using NUnit.Framework;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{
    [TestFixture]
    public class BasicPersistentDataManagerTest_Registering : BasicPersistentDataManagerTestContext
    {
        [Test]
        public void HasTheRegisterSerializersWithoutChannel()
        {
            manager.Register(serializerA);
            manager.Register(serializerB);

            Assert.That(manager.Contains(serializerA));
            Assert.That(manager.Contains(serializerB));
        }

        [Test]
        public void HasTheRegisterSerializersWithChannel()
        {
            manager.Register(serializerA, channel1);
            manager.Register(serializerB, channel2);

            Assert.That(manager.Contains(serializerA));
            Assert.That(manager.Contains(serializerB));
        }

        [Test]
        public void RegisteringASerializerOnTwoChannelsShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Register(serializerA, channel1);
                manager.Register(serializerA, channel2);
            }
            );

            Assert.That(action, Throws.Exception.InstanceOf<PersistentDataSerializerAlreadyRegisterException>());

            // TODO: Shoud I not assert that serializerA is not registered in channelB?
        }
    }
}