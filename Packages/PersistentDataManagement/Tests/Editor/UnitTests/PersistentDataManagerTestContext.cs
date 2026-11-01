using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataManagerTestContext
    {
        protected PersistentDataManager Manager;

        // The collaborators the default manager was built with. A test that wants to
        // observe one of them builds a new manager, passing the other one through.
        protected IPersistentDataIOStreamFactory EmptyStreamFactory;
        protected IPersistentDataEncoder EmptyDataEncoder;

        protected Mock<IPersistentDataSerializer> SerializerA;
        protected Mock<IPersistentDataSerializer> SerializerB;

        protected IChannel Channel1;
        protected IChannel Channel2;

        [SetUp]
        public void Setup()
        {
            SerializerA = Serializer("A");
            SerializerB = Serializer("B");

            Channel1 = new NamedChannel("ChannelA");
            Channel2 = new NamedChannel("ChannelB");

            EmptyStreamFactory = new EmptyPersistentDataIOStreamFactory();
            EmptyDataEncoder = new EmptyPersistentDataEncoder();

            Manager = CreateManager(EmptyStreamFactory, EmptyDataEncoder);

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

        // The manager chains its writes and asks the encoder for keys, so the
        // fluent methods have to answer with the encoder itself rather than null.
        protected static Mock<IPersistentDataEncoder> DataEncoder()
        {
            var encoder = new Mock<IPersistentDataEncoder>();

            encoder.Setup(w => w.HasKey(It.IsAny<string>())).Returns(true);
            encoder
                .Setup(w => w.WriteInt(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(() => encoder.Object);
            encoder
                .Setup(w => w.BeginWritingBlock(It.IsAny<string>()))
                .Returns(() => encoder.Object);
            encoder.Setup(w => w.EndWritingBlock()).Returns(() => encoder.Object);

            return encoder;
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
            IPersistentDataEncoder dataEncoder,
            int saveVersion = 0
        )
        {
            return new PersistentDataManager(streamFactory, dataEncoder, saveVersion);
        }

        protected virtual void InternalSetup() { }
    }
}
