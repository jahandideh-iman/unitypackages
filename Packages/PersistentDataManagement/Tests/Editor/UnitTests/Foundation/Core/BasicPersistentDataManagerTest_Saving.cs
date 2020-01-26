
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Mocks.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{
    [TestFixture]
    public class BasicPersistentDataManagerTest_Saving : BasicPersistentDataManagerTestContext
    {

        [Test]
        public void SavingAllShouldCallSerializeOnAllSerializers()
        {
            manager.Register(serializerA);
            manager.Register(serializerB, channelB);

            manager.SaveAll();

            Assert.That(serializerA.IsSerializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsSerializedCalledOnce(), Is.True);
        }

        [Test]
        public void SavingAChannelShoudCallSerializeOnAllTheRegisteredSerializersOnThatChannel()
        {
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.Save(channelA);

            Assert.That(serializerA.IsSerializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsSerializedCalledOnce(), Is.False);
        }

        [Test]
        public void SavingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Save(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(action, Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>());

            Assert.That(serializerA.IsSerialized(), Is.False);
            Assert.That(serializerB.IsSerialized(), Is.False);
        }

        [Test]
        public void SavingAllShouldClearPersistentDataWrapperForEachChannel()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            int clearCallCounts = 0;
            persistentDataWrapper.onClearAction = () => clearCallCounts++;

            manager.SetPersistentDataWrapper(persistentDataWrapper);
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.SaveAll();

            Assert.That(clearCallCounts, Is.EqualTo(2));
        }

        [Test]
        public void SavingAChannelShouldClearPersistentDataWrapper()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            int clearCallCounts = 0;
            persistentDataWrapper.onClearAction = () => clearCallCounts++;

            manager.SetPersistentDataWrapper(persistentDataWrapper);
            manager.Register(serializerA, channelA);


            manager.Save(channelA);

            Assert.That(clearCallCounts, Is.EqualTo(1));
        }

        [Test]
        public void SavingShouldGivePersistentDataWrapperToTheSerializers()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            var givenWrappers = new Dictionary<PersistentDataSerializer, WritablePersistentDataWrapper>();
            serializerA.onSerializeAction = (w) => givenWrappers.Add(serializerA, w);
            serializerB.onSerializeAction = (w) => givenWrappers.Add(serializerB, w);

            manager.SetPersistentDataWrapper(persistentDataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.SaveAll();
            
            Assert.That(givenWrappers[serializerA], Is.SameAs(persistentDataWrapper));
            Assert.That(givenWrappers[serializerB], Is.SameAs(persistentDataWrapper));
        }

        // TODO: Try to refactor this.
        [Test]
        public void SavingAllShouldWriteDataToPersistentDataWrapperAfterCallingAllSerializers()
        {
            var dataWrapper = new PersistentDataWrapperMock();

            int step = 0;
            int writeStep = -1;
            serializerA.onSerializeAction = (d) => step++;
            serializerB.onSerializeAction = (d) => step++;
            dataWrapper.onWriteAction = (w) => writeStep = step;

            manager.SetPersistentDataWrapper(dataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.SaveAll();

            Assert.That(writeStep, Is.EqualTo(2));
        }


        // TODO: Try to refactor this.
        [Test]
        public void SavingAChannelShouldWriteDataToPersistentDataWrapperAfterCallingChannelsSerializers()
        {
            var dataWrapper = new PersistentDataWrapperMock();

            int step = 0;
            int writeStep = -1;
            serializerA.onSerializeAction = (d) => step++;
            dataWrapper.onWriteAction = (w) => writeStep = step;

            manager.SetPersistentDataWrapper(dataWrapper);
            manager.Register(serializerA, channelA);

            manager.Save(channelA);

            Assert.That(writeStep, Is.EqualTo(1));
        }

        [Test]
        public void SavingAllShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForEachChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.SaveAll();

            Assert.That(streamFactory.CreateWriteStreamIsCalledOnceFor(channelA));
            Assert.That(streamFactory.CreateWriteStreamIsCalledOnceFor(channelB));
        }

        [Test]
        public void SavingAChannelShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForTheChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);

            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.Save(channelA);

            Assert.That(streamFactory.CreateWriteStreamIsCalledOnceFor(channelA), Is.True);
            Assert.That(streamFactory.CreateWriteStreamIsCalledOnceFor(channelB), Is.False);
        }
    }
}