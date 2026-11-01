using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    [TestFixture]
    public class PersistentDataManagerTest_Registering : PersistentDataManagerTestContext
    {
        [Test]
        public void HasTheRegisterSerializersWithoutChannel()
        {
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object);

            Assert.That(Manager.Contains(SerializerA.Object));
            Assert.That(Manager.Contains(SerializerB.Object));
        }

        [Test]
        public void HasTheRegisterSerializersWithChannel()
        {
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Assert.That(Manager.Contains(SerializerA.Object));
            Assert.That(Manager.Contains(SerializerB.Object));
        }

        [Test]
        public void RegisteringASerializerOnTwoChannelsShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                Manager.Register(SerializerA.Object, Channel1);
                Manager.Register(SerializerA.Object, Channel2);
            });

            Assert.That(
                action,
                Throws.Exception.InstanceOf<PersistentDataSerializerAlreadyRegisterException>()
            );

            // TODO: Should I not assert that serializerA is not registered in channelB?
        }
    }
}
