
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class BasicPersistentDataManagerTestContext
    {
        protected BasicPersistentDataManager manager;

        // The collaborators the default manager was built with. A test that wants to
        // observe one of them builds a new manager, passing the other one through.
        protected IPersistentDataIOStreamFactory emptyStreamFactory;
        protected IPersistentDataWrapper emptyDataWrapper;

        protected PersistentDataSerializerMock serializerA;
        protected PersistentDataSerializerMock serializerB;

        protected IChannel channel1;
        protected IChannel channel2;

        [SetUp]
        public void Setup()
        {
            serializerA = new PersistentDataSerializerMock("A");
            serializerB = new PersistentDataSerializerMock("B");

            channel1 = new NamedChannel("ChannelA");
            channel2 = new NamedChannel("ChannelB");

            emptyStreamFactory = new EmptyPersistetDataIOStreamFactory();
            emptyDataWrapper = new EmptyPersistentDataWrapper();

            manager = CreateManager(emptyStreamFactory, emptyDataWrapper);

            InternalSetup();
        }

        protected static BasicPersistentDataManager CreateManager(
            IPersistentDataIOStreamFactory streamFactory,
            IPersistentDataWrapper dataWrapper,
            int saveVersion = 0)
        {
            return new BasicPersistentDataManager(streamFactory, dataWrapper, saveVersion);
        }

        protected virtual void InternalSetup()
        {

        }
    }
}
