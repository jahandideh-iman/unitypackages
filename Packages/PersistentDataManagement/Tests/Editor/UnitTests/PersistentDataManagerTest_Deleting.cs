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

            manager = CreateManager(streamFactory.Object, emptyDataWrapper);

            manager.Delete(channel1);

            streamFactory.Verify(f => f.Delete(channel1), Times.Once);
        }

        [Test]
        public void DeletingAnUnregisteredChannelShouldNotThrow()
        {
            var action = new TestDelegate(() =>
            {
                manager.Delete(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(action, Throws.Nothing);
        }
    }
}
