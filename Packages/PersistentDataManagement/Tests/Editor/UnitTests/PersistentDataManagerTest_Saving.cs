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
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.SaveAll();

            VerifySerialized(SerializerA, Times.Once());
            VerifySerialized(SerializerB, Times.Once());
        }

        [Test]
        public void SavingAChannelShouldCallSerializeOnAllTheRegisteredSerializersOnThatChannel()
        {
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.Save(Channel1);

            VerifySerialized(SerializerA, Times.Once());
            VerifySerialized(SerializerB, Times.Never());
        }

        [Test]
        public void SavingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                Manager.Save(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(
                action,
                Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>()
            );

            VerifySerialized(SerializerA, Times.Never());
            VerifySerialized(SerializerB, Times.Never());
        }

        [Test]
        public void SavingAllShouldClearPersistentDataEncoderForEachChannel()
        {
            var persistentDataEncoder = DataEncoder();

            Manager = CreateManager(EmptyStreamFactory, persistentDataEncoder.Object);
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.SaveAll();

            persistentDataEncoder.Verify(w => w.Clear(), Times.Exactly(2));
        }

        [Test]
        public void SavingAChannelShouldClearPersistentDataEncoder()
        {
            var persistentDataEncoder = DataEncoder();

            Manager = CreateManager(EmptyStreamFactory, persistentDataEncoder.Object);
            Manager.Register(SerializerA.Object, Channel1);

            Manager.Save(Channel1);

            persistentDataEncoder.Verify(w => w.Clear(), Times.Once);
        }

        [Test]
        public void SavingShouldGivePersistentDataEncoderToTheSerializers()
        {
            var persistentDataEncoder = DataEncoder();

            Manager = CreateManager(EmptyStreamFactory, persistentDataEncoder.Object);
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object);

            Manager.SaveAll();

            SerializerA.Verify(s => s.SerializeTo(persistentDataEncoder.Object), Times.Once);
            SerializerB.Verify(s => s.SerializeTo(persistentDataEncoder.Object), Times.Once);
        }

        [Test]
        public void SavingAllShouldWriteDataToPersistentDataEncoderAfterCallingAllSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int writeStep = -1;
            SerializerA
                .Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()))
                .Callback(() => step++);
            SerializerB
                .Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.WriteTo(It.IsAny<StreamWriter>()))
                .Callback(() => writeStep = step);

            Manager = CreateManager(EmptyStreamFactory, dataEncoder.Object);
            Manager.Register(SerializerA.Object);
            Manager.Register(SerializerB.Object);

            Manager.SaveAll();

            Assert.That(writeStep, Is.EqualTo(2));
        }

        [Test]
        public void SavingAChannelShouldWriteDataToPersistentDataEncoderAfterCallingChannelsSerializers()
        {
            var dataEncoder = DataEncoder();

            int step = 0;
            int writeStep = -1;
            SerializerA
                .Setup(s => s.SerializeTo(It.IsAny<IWritablePersistentDataEncoder>()))
                .Callback(() => step++);
            dataEncoder
                .Setup(w => w.WriteTo(It.IsAny<StreamWriter>()))
                .Callback(() => writeStep = step);

            Manager = CreateManager(EmptyStreamFactory, dataEncoder.Object);
            Manager.Register(SerializerA.Object, Channel1);

            Manager.Save(Channel1);

            Assert.That(writeStep, Is.EqualTo(1));
        }

        [Test]
        public void SavingAllShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForEachChannel()
        {
            var streamFactory = StreamFactory();

            Manager = CreateManager(streamFactory.Object, EmptyDataEncoder);
            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.SaveAll();

            streamFactory.Verify(f => f.CreateWriteStreamFor(Channel1), Times.Once);
            streamFactory.Verify(f => f.CreateWriteStreamFor(Channel2), Times.Once);
        }

        [Test]
        public void SavingAChannelShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForTheChannel()
        {
            var streamFactory = StreamFactory();

            Manager = CreateManager(streamFactory.Object, EmptyDataEncoder);

            Manager.Register(SerializerA.Object, Channel1);
            Manager.Register(SerializerB.Object, Channel2);

            Manager.Save(Channel1);

            streamFactory.Verify(f => f.CreateWriteStreamFor(Channel1), Times.Once);
            streamFactory.Verify(f => f.CreateWriteStreamFor(Channel2), Times.Never);
        }
    }
}
