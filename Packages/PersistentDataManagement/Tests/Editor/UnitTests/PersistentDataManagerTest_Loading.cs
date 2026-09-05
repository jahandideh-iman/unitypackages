
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;
using System.IO;

namespace Arman.PersistentDataManagement.Tests
{
    [TestFixture]
    public class PersistentDataManagerTest_Loading : PersistentDataManagerTestContext
    {
        protected Mock<IPersistentDataSerializer> serializerC;

        static void VerifyDeserialized(Mock<IPersistentDataSerializer> serializer, Times times)
        {
            serializer.Verify(
                s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>()),
                times);
        }

        protected override void InternalSetup()
        {
            serializerC = Serializer("C");

            emptyStreamFactory = new MemoryBasedPersistetDataIOStreamFactory();
            emptyDataWrapper = new JSONPersistentDataWrapper();

            manager = CreateManager(emptyStreamFactory, emptyDataWrapper);
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
        public void LoadingAChannelShoudCallDeserializeOnAllTheRegisteredSerializersOnThatChannelThatAreSaved()
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

            manager = CreateManager(streamFactory, emptyDataWrapper);
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

            Assert.That(action, Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>());

            VerifyDeserialized(serializerA, Times.Never());
            VerifyDeserialized(serializerB, Times.Never());
        }

        [Test]
        public void LoadingShouldGivePersistentDataWrapperToTheSerializers()
        {
            var persistentDataWrapper = DataWrapper();

            manager = CreateManager(emptyStreamFactory, persistentDataWrapper.Object);
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);


            manager.LoadAll();

            serializerA.Verify(s => s.DeserializeFrom(persistentDataWrapper.Object), Times.Once);
            serializerB.Verify(s => s.DeserializeFrom(persistentDataWrapper.Object), Times.Once);
        }

        [Test]
        public void LoadingAllShouldClearPersistentDataWrapperForEachChannel()
        {
            var persistentDataWrapper = DataWrapper();

            manager = CreateManager(emptyStreamFactory, persistentDataWrapper.Object);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);


            manager.LoadAll();

            persistentDataWrapper.Verify(w => w.Clear(), Times.Exactly(2));
        }

        [Test]
        public void LoadingAChannelShouldClearPersistentDataWrapper()
        {
            var persistentDataWrapper = DataWrapper();

            manager = CreateManager(emptyStreamFactory, persistentDataWrapper.Object);
            manager.Register(serializerA.Object, channel1);


            manager.Load(channel1);

            persistentDataWrapper.Verify(w => w.Clear(), Times.Once);
        }

        [Test]
        public void LoadingAllShouldGiveDataToPersistentDataWrapperBeforeCallingAllSerializers()
        {
            var dataWrapper = DataWrapper();

            int step = 0;
            int readStep = -1;
            serializerA.Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>()))
                .Callback(() => step++);
            serializerB.Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>()))
                .Callback(() => step++);
            dataWrapper.Setup(w => w.ReadFrom(It.IsAny<StreamReader>()))
                .Callback(() => readStep = step);

            manager = CreateManager(emptyStreamFactory, dataWrapper.Object);
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);

            manager.LoadAll();

            Assert.That(readStep, Is.EqualTo(0));
        }


        [Test]
        public void LoadingAChannelShouldGiveDataToPersistentDataWrapperBeforeCallingChannelsSerializers()
        {
            var dataWrapper = DataWrapper();

            int step = 0;
            int readStep = -1;
            serializerA.Setup(s => s.DeserializeFrom(It.IsAny<IReadablePersistentDataWrapper>()))
                .Callback(() => step++);
            dataWrapper.Setup(w => w.ReadFrom(It.IsAny<StreamReader>()))
                .Callback(() => readStep = step);

            manager = CreateManager(emptyStreamFactory, dataWrapper.Object);
            manager.Register(serializerA.Object, channel1);

            manager.Load(channel1);

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAllShouldUsePersistentDataStreamFactoryToLoadReadStreamForEachChannel()
        {
            var streamFactory = StreamFactory();

            manager = CreateManager(streamFactory.Object, DataWrapper().Object);
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

            manager = CreateManager(streamFactory.Object, DataWrapper().Object);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.Load(channel1);

            streamFactory.Verify(f => f.CreateReadStreamFor(channel1), Times.Once);
            streamFactory.Verify(f => f.CreateReadStreamFor(channel2), Times.Never);
        }
    }
}
