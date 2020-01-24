
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Mocks.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{


    [TestFixture]
    public class BasicPersistentDataManagerSavingFuncionalityTest : BasicPersistentDataManagerTestContext
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

            manager.SetPersistentDataHandler(persistentDataWrapper);
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

            manager.SetPersistentDataHandler(persistentDataWrapper);
            manager.Register(serializerA, channelA);


            manager.Save(channelA);

            Assert.That(clearCallCounts, Is.EqualTo(1));
        }

        [Test]
        public void SavingShouldGivePersistentDataWrapperToTheSerializers()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            var givenWrappers = new Dictionary<PersistentDataSerializer, PersistentDataWrapper>();
            serializerA.onSerializeAction = (w) => givenWrappers.Add(serializerA, w);
            serializerB.onSerializeAction = (w) => givenWrappers.Add(serializerB, w);

            manager.SetPersistentDataHandler(persistentDataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.SaveAll();
            
            Assert.That(givenWrappers[serializerA], Is.SameAs(persistentDataWrapper));
            Assert.That(givenWrappers[serializerB], Is.SameAs(persistentDataWrapper));
        }

        // TODO: Try to refactor this.
        [Test]
        public void SavingAllShouldCallPersistentDataWrapperAfterCallingAllSerializers()
        {
            var dataWrapper = new PersistentDataWrapperMock();

            bool writeIsCalledLast = false;
            serializerA.onSerializeAction = (d) => writeIsCalledLast = false;
            serializerB.onSerializeAction = (d) => writeIsCalledLast = false;
            dataWrapper.onWriteAction = (w) => writeIsCalledLast = true;

            manager.SetPersistentDataHandler(dataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.SaveAll();

            Assert.That(writeIsCalledLast);
        }


        // TODO: Try to refactor this.
        [Test]
        public void SavingAChannelShouldCallPersistentDataWrapperAfterCallingChannelsSerializers()
        {
            var dataWrapper = new PersistentDataWrapperMock();

            bool writeIsCalledLast = false;
            serializerA.onSerializeAction = (d) => writeIsCalledLast = false;
            dataWrapper.onWriteAction = (w) => writeIsCalledLast = true;

            manager.SetPersistentDataHandler(dataWrapper);
            manager.Register(serializerA, channelA);

            manager.Save(channelA);

            Assert.That(writeIsCalledLast);
        }

        [Test]
        public void SavingAllShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForEachChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.SaveAll();

            Assert.That(streamFactory.CreateIsCalledOnceFor(channelA));
            Assert.That(streamFactory.CreateIsCalledOnceFor(channelB));
        }

        [Test]
        public void SavingAChannelShouldUsePersistentDataStreamFactoryToCreateANewWriteStreamForTheChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);

            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.Save(channelA);

            Assert.That(streamFactory.CreateIsCalledOnceFor(channelA), Is.True);
            Assert.That(streamFactory.CreateIsCalledOnceFor(channelB), Is.False);
        }
    }
}