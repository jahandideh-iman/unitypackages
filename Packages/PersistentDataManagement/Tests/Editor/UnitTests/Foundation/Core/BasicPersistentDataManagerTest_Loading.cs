
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Foundation.Unity.PersistentDataManagement;
using Arman.Mocks.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;
using System.Collections.Generic;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{
    [TestFixture]
    public class BasicPersistentDataManagerTest_Loading : BasicPersistentDataManagerTestContext
    {
        protected PersistentDataSerializerMock serializerC;


        protected override void InternalSetup()
        {
            serializerC = new PersistentDataSerializerMock("C");

            manager.SetPersistentDataIOStreamFactory(new MemoryBasedPersistetDataIOStreamFactory());
            manager.SetPersistentDataWrapper(new JSONPersistentDataWrapper());
        }

        [Test]
        public void LoadingAllShouldCallDeserializerOnAllSerializersThatAreSaved()
        {
            manager.Register(serializerA);
            manager.Register(serializerB, channel2);
            manager.SaveAll();
            manager.Register(serializerC);

            manager.LoadAll();

            Assert.That(serializerA.IsDeserializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsDeserializedCalledOnce(), Is.True);
            Assert.That(serializerC.IsDeserialized(), Is.False);
        }

        [Test]
        public void LoadingAChannelShoudCallDeserializeOnAllTheRegisteredSerializersOnThatChannelThatAreSaved()
        {
            manager.Register(serializerA, channel1);
            manager.Register(serializerB, channel2);
            manager.SaveAll();
            manager.Register(serializerC, channel1);

            manager.Load(channel1);

            Assert.That(serializerA.IsDeserializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsDeserialized(), Is.False);
            Assert.That(serializerC.IsDeserialized(), Is.False);
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
            manager.Register(serializerA);
            manager.Register(serializerB);
            manager.SaveAll();

            var persistentDataWrapper = new PersistentDataWrapperMock();

            var givenWrappers = new Dictionary<PersistentDataSerializer, ReadablePersistentDataWrapper>();
            serializerA.onDeserializeAction = (w) => givenWrappers.Add(serializerA, w);
            serializerB.onDeserializeAction = (w) => givenWrappers.Add(serializerB, w);

            manager.SetPersistentDataWrapper(persistentDataWrapper);


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
            manager.Register(serializerA, channel1);
            manager.Register(serializerB, channel2);


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
            manager.Register(serializerA, channel1);


            manager.Load(channel1);

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
            manager.Register(serializerA, channel1);

            manager.Load(channel1);

            Assert.That(readStep, Is.EqualTo(0));
        }

        [Test]
        public void LoadingAllShouldUsePersistentDataStreamFactoryToLoadReadStreamForEachChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);
            manager.SetPersistentDataWrapper(new PersistentDataWrapperMock());
            manager.Register(serializerA, channel1);
            manager.Register(serializerB, channel2);

            manager.LoadAll();

            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channel1));
            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channel2));
        }

        [Test]
        public void LoadingAChannelShouldUsePersistentDataStreamFactoryToLoadReadStreamForTheChannel()
        {
            var streamFactory = new PersistentDataIOStreamFactoryMock();

            manager.SetPersistentDataIOStreamFactory(streamFactory);
            manager.SetPersistentDataWrapper(new PersistentDataWrapperMock());
            manager.Register(serializerA, channel1);
            manager.Register(serializerB, channel2);

            manager.Load(channel1);

            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channel1), Is.True);
            Assert.That(streamFactory.CreateReadStreamIsCalledOnceFor(channel2), Is.False);
        }
    }
}