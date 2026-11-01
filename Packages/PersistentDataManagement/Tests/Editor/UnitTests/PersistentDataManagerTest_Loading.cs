using System.IO;
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    [TestFixture]
    public class PersistentDataManagerTest_Loading : PersistentDataManagerTestContext
    {
        protected Mock<IPersistentDataSerializer> SerializerC;

        static void VerifyDeserialized(Mock<IPersistentDataSerializer> serializer, Times times)
        {
            serializer.Verify(
                s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()),
                times
            );
        }

        protected override void InternalSetup()
        {
            SerializerC = Serializer("C");

            EmptyStreamFactory = new MemoryBasedPersistentDataIOStreamFactory();
            EmptyDataEncoder = new JSONPersistentDataEncoder();

            Manager = CreateManager(EmptyStreamFactory, EmptyDataEncoder);
        }

        [Test]
        public void LoadingAllShouldCallDeserializerOnAllSerializersThatAreSaved()
        {
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object, Channel2);
            Manager.SaveAll();
            Manager.Register(SerializerC.Object);

            Manager.LoadAll();

            VerifyDeserialized(SerializerA, Times.Once());
            VerifyDeserialized(SerializerB, Times.Once());
            VerifyDeserialized(SerializerC, Times.Never());
        }

        [Test]
        public void LoadingAChannelShouldCallDeserializeOnAllTheRegisteredSerializersOnThatChannelThatAreSaved()
        {
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);
            Manager.SaveAll();
            Manager.Register(SerializerC.Object, Channel1);

            Manager.Load(Channel1);

            VerifyDeserialized(SerializerA, Times.Once());
            VerifyDeserialized(SerializerB, Times.Never());
            VerifyDeserialized(SerializerC, Times.Never());
        }

        [Test]
        public void LoadingAChannelThatWasNeverSavedShouldNotThrow()
        {
            Manager.Register(SerializerA.Object, Channel1);

            Assert.DoesNotThrow(() => Manager.Load(Channel1));
            VerifyDeserialized(SerializerA, Times.Never());
        }

        [Test]
        public void LoadingAChannelWhoseStoredDataIsEmptyShouldNotThrow()
        {
            var streamFactory = new MemoryBasedPersistentDataIOStreamFactory();
            streamFactory.CreateWriteStreamFor(Channel1).Dispose();

            Manager = CreateManager(streamFactory, EmptyDataEncoder);
            Manager.Register(SerializerA.Object, Channel1);

            Assert.DoesNotThrow(() => Manager.Load(Channel1));
            VerifyDeserialized(SerializerA, Times.Never());
        }

        [Test]
        public void LoadingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                Manager.Load(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(
                action,
                Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>()
            );

            VerifyDeserialized(SerializerA, Times.Never());
            VerifyDeserialized(SerializerB, Times.Never());
        }

        [Test]
        public void LoadingShouldGivePersistentDataEncoderToTheSerializers()
        {
            var persistentDataEncoder = DataEncoder();

            Manager = CreateManager(EmptyStreamFactory, persistentDataEncoder.Object);
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object);

            Manager.LoadAll();

            SerializerA.Verify(s => s.DeserializeFrom(persistentDataEncoder.Object), Times.Once);
            SerializerB.Verify(s => s.DeserializeFrom(persistentDataEncoder.Object), Times.Once);
        }

        [Test]
        public void LoadingAllShouldClearPersistentDataEncoderForEachChannel()
        {
            var persistentDataEncoder = DataEncoder();

            Manager = CreateManager(EmptyStreamFactory, persistentDataEncoder.Object);
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.LoadAll();

            persistentDataEncoder.Verify(w => w.Clear(), Times.Exactly(2));
        }

        [Test]
        public void LoadingAChannelShouldClearPersistentDataEncoder()
        {
            var persistentDataEncoder = DataEncoder();

            Manager = CreateManager(EmptyStreamFactory, persistentDataEncoder.Object);
            Manager.Register(SerializerA.Object, Channel1);

            Manager.Load(Channel1);

            persistentDataEncoder.Verify(w => w.Clear(), Times.Once);
        }

        [Test]
        public void LoadingAllShouldGiveDataToPersistentDataEncoderBeforeCallingAllSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int readStep = -1;
            SerializerA
                .Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()))
                .Callback(() => step++);
            SerializerB
                .Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.ReadFrom(It.IsAny<StreamReader>()))
                .Callback(() => readStep = step);

            Manager = CreateManager(EmptyStreamFactory, dataEncoder.Object);
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object);

            Manager.LoadAll();

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAChannelShouldGiveDataToPersistentDataEncoderBeforeCallingChannelsSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int readStep = -1;
            SerializerA
                .Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.ReadFrom(It.IsAny<StreamReader>()))
                .Callback(() => readStep = step);

            Manager = CreateManager(EmptyStreamFactory, dataEncoder.Object);
            Manager.Register(SerializerA.Object, Channel1);

            Manager.Load(Channel1);

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAllShouldUsePersistentDataStreamFactoryToLoadReadStreamForEachChannel()
        {
            var streamFactory = StreamFactory();

            Manager = CreateManager(streamFactory.Object, DataEncoder().Object);
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.LoadAll();

            streamFactory.Verify(f => f.CreateReadStreamFor(Channel1), Times.Once);
            streamFactory.Verify(f => f.CreateReadStreamFor(Channel2), Times.Once);
        }

        [Test]
        public void LoadingAChannelShouldUsePersistentDataStreamFactoryToLoadReadStreamForTheChannel()
        {
            var streamFactory = StreamFactory();

            Manager = CreateManager(streamFactory.Object, DataEncoder().Object);
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.Load(Channel1);

            streamFactory.Verify(f => f.CreateReadStreamFor(Channel1), Times.Once);
            streamFactory.Verify(f => f.CreateReadStreamFor(Channel2), Times.Never);
        }
    }
}
