
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

        protected Channel channel1;
        protected Channel channel2;

        [SetUp]
        public void Setup()
        {
            manager = new BasicPersistentDataManager();

            serializerA = new PersistentDataSerializerMock("A");
            serializerB = new PersistentDataSerializerMock("B");

            channel1 = new NamedChannel("ChannelA");
            channel2 = new NamedChannel("ChannelB");

            manager.SetPersistentDataWrapper(new EmptyPersistentDataWrapper());
            manager.SetPersistentDataIOStreamFactory(new EmptyPersistetDataIOStreamFactory());

            InternalSetup();
        }

        protected virtual void InternalSetup()
        {

        }
    }
}