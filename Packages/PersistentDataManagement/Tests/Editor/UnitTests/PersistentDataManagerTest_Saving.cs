using System.IO;
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    [TestFixture]
    public class PersistentDataManagerTest_Saving : PersistentDataManagerTestContext
    {
        static void VerifySerialized(Mock<IPersistentDataSerializer> serializer, Times times)
        {
            serializer.Verify(
                s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()),
                times
            );
        }

        [Test]
        public void SavingAllShouldCallSerializeOnAllSerializers()
        {
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object, channel2);

            manager.SaveAll();

            VerifySerialized(serializerA, Times.Once());
            VerifySerialized(serializerB, Times.Once());
        }

        [Test]
        public void SavingAChannelShouldCallSerializeOnAllTheRegisteredSerializersOnThatChannel()
        {
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.Save(channel1);

            VerifySerialized(serializerA, Times.Once());
            VerifySerialized(serializerB, Times.Never());
        }

        [Test]
        public void SavingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Save(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(
                action,
                Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>()
            );

            VerifySerialized(serializerA, Times.Never());
            VerifySerialized(serializerB, Times.Never());
        }

        [Test]
        public void SavingAllShouldClearPersistentDataEncoderForEachChannel()
        {
            var persistentDataEncoder = DataEncoder();

            manager = CreateManager(emptyStreamFactory, persistentDataEncoder.Object);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.SaveAll();

            persistentDataEncoder.Verify(w => w.Clear(), Times.Exactly(2));
        }

        [Test]
        public void SavingAChannelShouldClearPersistentDataEncoder()
        {
            var persistentDataEncoder = DataEncoder();

            manager = CreateManager(emptyStreamFactory, persistentDataEncoder.Object);
            manager.Register(serializerA.Object, channel1);

            manager.Save(channel1);

            persistentDataEncoder.Verify(w => w.Clear(), Times.Once);
        }

        [Test]
        public void SavingShouldGivePersistentDataEncoderToTheSerializers()
        {
            var persistentDataEncoder = DataEncoder();

            manager = CreateManager(emptyStreamFactory, persistentDataEncoder.Object);
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);

            manager.SaveAll();

            serializerA.Verify(s => s.SerializeTo(persistentDataEncoder.Object), Times.Once);
            serializerB.Verify(s => s.SerializeTo(persistentDataEncoder.Object), Times.Once);
        }

        [Test]
        public void SavingAllShouldWriteDataToPersistentDataEncoderAfterCallingAllSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int writeStep = -1;
            serializerA
                .Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()))
                .Callback(() => step++);
            serializerB
                .Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.WriteTo(It.IsAny<StreamWriter>()))
                .Callback(() => writeStep = step);

            manager = CreateManager(emptyStreamFactory, dataEncoder.Object);
            manager.Register(serializerA.Object);
            manager.Register(serializerB.Object);

            manager.SaveAll();

            Assert.That(writeStep, Is.EqualTo(2));
        }

        [Test]
        public void SavingAChannelShouldWriteDataToPersistentDataEncoderAfterCallingChannelsSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int writeStep = -1;
            serializerA
                .Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.WriteTo(It.IsAny<StreamWriter>()))
                .Callback(() => writeStep = step);

            manager = CreateManager(emptyStreamFactory, dataEncoder.Object);
            manager.Register(serializerA.Object, channel1);

            manager.Save(channel1);

            Assert.That(writeStep, Is.EqualTo(1));
        }

        [Test]
        public void SavingAllShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForEachChannel()
        {
            var streamFactory = StreamFactory();

            manager = CreateManager(streamFactory.Object, emptyDataEncoder);
            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.SaveAll();

            streamFactory.Verify(f => f.CreateWriteStreamFor(channel1), Times.Once);
            streamFactory.Verify(f => f.CreateWriteStreamFor(channel2), Times.Once);
        }

        [Test]
        public void SavingAChannelShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForTheChannel()
        {
            var streamFactory = StreamFactory();

            manager = CreateManager(streamFactory.Object, emptyDataEncoder);

            manager.Register(serializerA.Object, channel1);
            manager.Register(serializerB.Object, channel2);

            manager.Save(channel1);

            streamFactory.Verify(f => f.CreateWriteStreamFor(channel1), Times.Once);
            streamFactory.Verify(f => f.CreateWriteStreamFor(channel2), Times.Never);
        }
    }
}
