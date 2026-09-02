
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class BasicPersistentDataManagerTestContext
    {
        protected BasicPersistentDataManager manager;

        protected PersistentDataSerializerMock serializerA;
        protected PersistentDataSerializerMock serializerB;

        protected IChannel channel1;
        protected IChannel channel2;

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