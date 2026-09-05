
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataManagerTestContext
    {
        protected PersistentDataManager manager;

        // The collaborators the default manager was built with. A test that wants to
        // observe one of them builds a new manager, passing the other one through.
        protected IPersistentDataIOStreamFactory emptyStreamFactory;
        protected IPersistentDataWrapper emptyDataWrapper;

        protected Mock<IPersistentDataSerializer> serializerA;
        protected Mock<IPersistentDataSerializer> serializerB;

        protected IChannel channel1;
        protected IChannel channel2;

        [SetUp]
        public void Setup()
        {
            serializerA = Serializer("A");
            serializerB = Serializer("B");

            channel1 = new NamedChannel("ChannelA");
            channel2 = new NamedChannel("ChannelB");

            emptyStreamFactory = new EmptyPersistetDataIOStreamFactory();
            emptyDataWrapper = new EmptyPersistentDataWrapper();

            manager = CreateManager(emptyStreamFactory, emptyDataWrapper);

            InternalSetup();
        }

        // The manager reads a serializer's key to name its block, so a loose mock
        // answering null is not enough.
        protected static Mock<IPersistentDataSerializer> Serializer(string key)
        {
            var serializer = new Mock<IPersistentDataSerializer>();
            serializer.Setup(s => s.Key()).Returns(key);
            return serializer;
        }

        // The manager chains its writes and asks the wrapper for keys, so the
        // fluent methods have to answer with the wrapper itself rather than null.
        protected static Mock<IPersistentDataWrapper> DataWrapper()
        {
            var wrapper = new Mock<IPersistentDataWrapper>();

            wrapper.Setup(w => w.HasKey(It.IsAny<string>())).Returns(true);
            wrapper.Setup(w => w.WriteInt(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(() => wrapper.Object);
            wrapper.Setup(w => w.BeginWritingBlock(It.IsAny<string>()))
                .Returns(() => wrapper.Object);
            wrapper.Setup(w => w.EndWritingBlock())
                .Returns(() => wrapper.Object);

            return wrapper;
        }

        // Reports every channel as readable; the streams themselves stay null,
        // which is what the empty factory hands back too.
        protected static Mock<IPersistentDataIOStreamFactory> StreamFactory()
        {
            var streamFactory = new Mock<IPersistentDataIOStreamFactory>();
            streamFactory.Setup(f => f.HasReadableStreamFor(It.IsAny<IChannel>())).Returns(true);
            return streamFactory;
        }

        protected static PersistentDataManager CreateManager(
            IPersistentDataIOStreamFactory streamFactory,
            IPersistentDataWrapper dataWrapper,
            int saveVersion = 0)
        {
            return new PersistentDataManager(streamFactory, dataWrapper, saveVersion);
        }

        protected virtual void InternalSetup()
        {

        }
    }
}
