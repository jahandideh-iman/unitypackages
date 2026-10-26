using System.IO;
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    [TestFixture]
    public class PersistentDataManagerTest_Loading : PersistentDataManagerTestContext
    {
        protected Mock<IPersistentDataSerializer> serializerC;

        static void VerifyDeserialized(Mock<IPersistentDataSerializer> serializer, Times times)
        {
            serializer.Verify(
                s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()),
                times
            );
        }

        protected override void InternalSetup()
        {
            serializerC = Serializer("C");

            emptyStreamFactory = new MemoryBasedPersistetDataIOStreamFactory();
            emptyDataEncoder = new JSONPersistentDataEncoder();

            manager = CreateManager(emptyStreamFactory, emptyDataEncoder);
        }

        [Test]
        public void LoadingAllShouldCallDeserializerOnAllSerializersThatAreSaved()
        {
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object, channel2);
            manager.SaveAll();
            manager.Register(serializerC.Object);

            manager.LoadAll();

            VerifyDeserialized(serializerA, Times.Once());
            VerifyDeserialized(serializerB, Times.Once());
            VerifyDeserialized(serializerC, Times.Never());
        }

        [Test]
        public void LoadingAChannelShouldCallDeserializeOnAllTheRegisteredSerializersOnThatChannelThatAreSaved()
        {
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);
            manager.SaveAll();
            manager.Register(serializerC.Object, channel1);

            manager.Load(channel1);

            VerifyDeserialized(serializerA, Times.Once());
            VerifyDeserialized(serializerB, Times.Never());
            VerifyDeserialized(serializerC, Times.Never());
        }

        [Test]
        public void LoadingAChannelThatWasNeverSavedShouldNotThrow()
        {
            manager.Register(serializerA.Object, channel1);

            Assert.DoesNotThrow(() => manager.Load(channel1));
            VerifyDeserialized(serializerA, Times.Never());
        }

        [Test]
        public void LoadingAChannelWhoseStoredDataIsEmptyShouldNotThrow()
        {
            var streamFactory = new MemoryBasedPersistetDataIOStreamFactory();
            streamFactory.CreateWriteStreamFor(channel1).Dispose();

            manager = CreateManager(streamFactory, emptyDataEncoder);
            manager.Register(serializerA.Object, channel1);

            Assert.DoesNotThrow(() => manager.Load(channel1));
            VerifyDeserialized(serializerA, Times.Never());
        }

        [Test]
        public void LoadingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Load(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(
                action,
                Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>()
            );

            VerifyDeserialized(serializerA, Times.Never());
            VerifyDeserialized(serializerB, Times.Never());
        }

        [Test]
        public void LoadingShouldGivePersistentDataEncoderToTheSerializers()
        {
            var persistentDataEncoder = DataEncoder();

            manager = CreateManager(emptyStreamFactory, persistentDataEncoder.Object);
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);

            manager.LoadAll();

            serializerA.Verify(s => s.DeserializeFrom(persistentDataEncoder.Object), Times.Once);
            serializerB.Verify(s => s.DeserializeFrom(persistentDataEncoder.Object), Times.Once);
        }

        [Test]
        public void LoadingAllShouldClearPersistentDataEncoderForEachChannel()
        {
            var persistentDataEncoder = DataEncoder();

            manager = CreateManager(emptyStreamFactory, persistentDataEncoder.Object);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.LoadAll();

            persistentDataEncoder.Verify(w => w.Clear(), Times.Exactly(2));
        }

        [Test]
        public void LoadingAChannelShouldClearPersistentDataEncoder()
        {
            var persistentDataEncoder = DataEncoder();

            manager = CreateManager(emptyStreamFactory, persistentDataEncoder.Object);
            manager.Register(serializerA.Object, channel1);

            manager.Load(channel1);

            persistentDataEncoder.Verify(w => w.Clear(), Times.Once);
        }

        [Test]
        public void LoadingAllShouldGiveDataToPersistentDataEncoderBeforeCallingAllSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int readStep = -1;
            serializerA
                .Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()))
                .Callback(() => step++);
            serializerB
                .Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.ReadFrom(It.IsAny<StreamReader>()))
                .Callback(() => readStep = step);

            manager = CreateManager(emptyStreamFactory, dataEncoder.Object);
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);

            manager.LoadAll();

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAChannelShouldGiveDataToPersistentDataEncoderBeforeCallingChannelsSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int readStep = -1;
            serializerA
                .Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.ReadFrom(It.IsAny<StreamReader>()))
                .Callback(() => readStep = step);

            manager = CreateManager(emptyStreamFactory, dataEncoder.Object);
            manager.Register(serializerA.Object, channel1);

            manager.Load(channel1);

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAllShouldUsePersistentDataStreamFactoryToLoadReadStreamForEachChannel()
        {
            var streamFactory = StreamFactory();

            manager = CreateManager(streamFactory.Object, DataEncoder().Object);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.LoadAll();

            streamFactory.Verify(f => f.CreateReadStreamFor(channel1), Times.Once);
            streamFactory.Verify(f => f.CreateReadStreamFor(channel2), Times.Once);
        }

        [Test]
        public void LoadingAChannelShouldUsePersistentDataStreamFactoryToLoadReadStreamForTheChannel()
        {
            var streamFactory = StreamFactory();

            manager = CreateManager(streamFactory.Object, DataEncoder().Object);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.Load(channel1);

            streamFactory.Verify(f => f.CreateReadStreamFor(channel1), Times.Once);
            streamFactory.Verify(f => f.CreateReadStreamFor(channel2), Times.Never);
        }
    }
}
