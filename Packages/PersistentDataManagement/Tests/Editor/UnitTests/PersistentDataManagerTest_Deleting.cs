using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataManagerTest_Deleting : PersistentDataManagerTestContext
    {
        [Test]
        public void DeletingAChannelShouldUsePersistentDataStreamFactoryToDeleteTheChannel()
        {
            var streamFactory = StreamFactory();

            Manager = CreateManager(streamFactory.Object, EmptyDataEncoder);

            Manager.Delete(Channel1);

            streamFactory.Verify(f => f.Delete(Channel1), Times.Once);
        }

        [Test]
        public void DeletingAnUnregisteredChannelShouldNotThrow()
        {
            var action = new TestDelegate(() =>
            {
                Manager.Delete(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(action, Throws.Nothing);
        }
    }
}
