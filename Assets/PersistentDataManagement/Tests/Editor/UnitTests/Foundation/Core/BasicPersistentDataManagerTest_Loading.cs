
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Mocks.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{
    [TestFixture]
    public class BasicPersistentDataManagerTest_Loading : BasicPersistentDataManagerTestContext
    {
        [Test]
        public void LoadingAllShouldCallDeserializerOnAllSerializers()
        {
            manager.Register(serializerA);
            manager.Register(serializerB, channelB);

            manager.LoadAll();

            Assert.That(serializerA.IsDeserializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsDeserializedCalledOnce(), Is.True);
        }

        [Test]
        public void LoadingAChannelShoudCallDeserializeOnAllTheRegisteredSerializersOnThatChannel()
        {
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.Load(channelA);

            Assert.That(serializerA.IsDeserializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsDeserializedCalledOnce(), Is.False);
        }

        [Test]
        public void LoadingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Load(new NamedChannel("UnregisteredChannel"));
            });

            Assert.That(action, Throws.Exception.InstanceOf<PersistentDataChannelNotFoundException>());

            Assert.That(serializerA.IsDeserialized(), Is.False);
            Assert.That(serializerB.IsDeserialized(), Is.False);
        }

        [Test]
        public void LoadingShouldGivePersistentDataWrapperToTheSerializers()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            var givenWrappers = new Dictionary<PersistentDataSerializer, ReadablePersistentDataWrapper>();
            serializerA.onDeserializeAction = (w) => givenWrappers.Add(serializerA, w);
            serializerB.onDeserializeAction = (w) => givenWrappers.Add(serializerB, w);

            manager.SetPersistentDataWrapper(persistentDataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.LoadAll();

            Assert.That(givenWrappers[serializerA], Is.SameAs(persistentDataWrapper));
            Assert.That(givenWrappers[serializerB], Is.SameAs(persistentDataWrapper));
        }

        [Test]
        public void LoadingAllShouldClearPersistentDataWrapperForEachChannel()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            int clearCallCounts = 0;
            persistentDataWrapper.onClearAction = () => clearCallCounts++;

            manager.SetPersistentDataWrapper(persistentDataWrapper);
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.LoadAll();

            Assert.That(clearCallCounts, Is.EqualTo(2));
        }

        [Test]
        public void LoadingAChannelShouldClearPersistentDataWrapper()
        {
            var persistentDataWrapper = new PersistentDataWrapperMock();

            int clearCallCounts = 0;
            persistentDataWrapper.onClearAction = () => clearCallCounts++;

            manager.SetPersistentDataWrapper(persistentDataWrapper);
            manager.Register(serializerA, channelA);


            manager.Load(channelA);

            Assert.That(clearCallCounts, Is.EqualTo(1));
        }

        // TODO: Try to refactor this.
        [Test]
        public void LoadingAllShouldGiveDataToPersistentDataWrapperBeforeCallingAllSerializers()
        {
            var dataWrapper = new PersistentDataWrapperMock();

            int step = 0;
            int readStep = -1;
            serializerA.onDeserializeAction = (d) => step++;
            serializerB.onDeserializeAction = (d) => step++;
            dataWrapper.onReadAction = (s) => readStep = step;

            manager.SetPersistentDataWrapper(dataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.LoadAll();

            Assert.That(readStep, Is.EqualTo(0));
        }


        // TODO: Try to refactor this.
        [Test]
        public void LoadingAChannelShouldGiveDataToPersistentDataWrapperBeforeCallingChannelsSerializers()
        {
            var dataWrapper = new PersistentDataWrapperMock();

            int step = 0;
            int readStep = -1;
            serializerA.onDeserializeAction = (d) => step++;
            dataWrapper.onReadAction = (s) => readStep = step;

            manager.SetPersistentDataWrapper(dataWrapper);
            manager.Register(serializerA, channelA);

            manager.Load(channelA);

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAllShouldUsePersistentDataStreamFactoryToLoadReadStreamForEachChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.LoadAll();

            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channelA));
            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channelB));
        }

        [Test]
        public void LoadingAChannelShouldUsePersistentDataStreamFactoryToLoadReadStreamForTheChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);

            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.Load(channelA);

            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channelA), Is.True);
            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channelB), Is.False);
        }
    }
}