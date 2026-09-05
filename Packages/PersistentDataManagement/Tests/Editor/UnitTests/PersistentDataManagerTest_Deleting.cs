using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataManagerTest_Deleting : PersistentDataManagerTestContext
    {
        [Test]
        public void DeletingAChannelShouldUsePersistentDataStreamFactoryToDeleteTheChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager = CreateManager(streamFactory, emptyDataWrapper);

            manager.Delete(channel1);

            Assert.That(streamFactory.DeleteIsCalledOnceFor(channel1), Is.True);
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