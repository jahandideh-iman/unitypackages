
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Mocks.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{
    public class BasicPersistentDataManagerTestContext
    {
        protected BasicPersistentDataManager manager;

        protected PersistentDataSerializerMock serializerA;
        protected PersistentDataSerializerMock serializerB;

        protected Channel channelA;
        protected Channel channelB;

        [SetUp]
        public void Setup()
        {
            manager = new BasicPersistentDataManager();

            serializerA = new PersistentDataSerializerMock();
            serializerB = new PersistentDataSerializerMock();

            channelA = new NamedChannel("ChannelA");
            channelB = new NamedChannel("ChannelB");

            manager.SetPersistentDataWrapper(new EmptyPersistentDataWrapper());
            manager.SetPersistentDataIOStreamFactory(new EmptyPersistetDataIOStreamFactory());
        }
    }
}