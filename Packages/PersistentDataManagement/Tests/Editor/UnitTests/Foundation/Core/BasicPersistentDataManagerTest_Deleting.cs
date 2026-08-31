using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Mocks.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{
    [TestFixture]
    public class BasicPersistentDataManagerTest_Deleting : BasicPersistentDataManagerTestContext
    {
        [Test]
        public void DeletingAChannelShouldUsePersistentDataStreamFactoryToDeleteTheChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);

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