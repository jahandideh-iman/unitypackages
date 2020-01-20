
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;
using System;

namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{

    public class FakeSerializer : PersistentDataSerializer
    {
        int serializedCalls;
        public PersistentDataWrapper givenWrapper;

        public bool IsSerializedCalledOnce()
        {
            return serializedCalls == 1;
        }

        public bool IsSerialized()
        {
            return serializedCalls > 0;
        }

        public void Serialize(PersistentDataWrapper persistentDataWrapper)
        {
            givenWrapper = persistentDataWrapper;
            serializedCalls++;
        }
    }

    public class EmptyPersistentDataWrapper : PersistentDataWrapper
    {
        public void SaveInt(string key, int value)
        {
            throw new System.NotImplementedException();
        }

        public void SaveString(string key, string value)
        {
            throw new System.NotImplementedException();
        }
    }


    public class BasicPersistentDataManagerTest
    {

        BasicPersistentDataManager manager;

        FakeSerializer serializerA;
        FakeSerializer serializerB;

        Channel channelA;
        Channel channelB;

        [SetUp]
        public void Setup()
        {
            manager = new BasicPersistentDataManager();

            serializerA = new FakeSerializer();
            serializerB = new FakeSerializer();

            channelA = new NamedChannel("ChannelA");
            channelB = new NamedChannel("ChannelB");
        }

        [Test]
        public void HasTheRegisteredSerializers()
        {
            manager.Register(serializerA);
            manager.Register(serializerB, channelB);

            Assert.That(manager.Contains(serializerA));
            Assert.That(manager.Contains(serializerB));
        }

        [Test]
        public void SaveAllShouldCallSerializeOnAllSerializers()
        {
            manager.Register(serializerA);
            manager.Register(serializerB, channelB);

            manager.SaveAll();

            Assert.That(serializerA.IsSerializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsSerializedCalledOnce(), Is.True);
        }

        [Test]
        public void SaveAChannelShoudCallSerializeOnAllTheRegisteredSerializersOnThatChannel()
        {
            manager.Register(serializerA, channelA);
            manager.Register(serializerB, channelB);

            manager.Save(channelA);

            Assert.That(serializerA.IsSerializedCalledOnce(), Is.True);
            Assert.That(serializerB.IsSerializedCalledOnce(), Is.False);
        }

        [Test]
        public void SaveingShouldGivePersistentDataWrapperToSerializers()
        {
            var persistentDataWrapper = new EmptyPersistentDataWrapper();
            manager.SetPersistentDataHandler(persistentDataWrapper);
            manager.Register(serializerA);
            manager.Register(serializerB);

            manager.SaveAll();

            Assert.That(serializerA.givenWrapper, Is.SameAs(persistentDataWrapper));
            Assert.That(serializerB.givenWrapper, Is.SameAs(persistentDataWrapper));
        }


        [Test]
        public void SavingAnUnregisterChannelShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
            {
                manager.Save(new NamedChannel("UnregisteredChannel"));
            });

            Assert.Throws(
                Is.InstanceOf<PersistentDataChannelNotFoundException>(),
                action);

            Assert.That(serializerA.IsSerialized(), Is.False);
            Assert.That(serializerB.IsSerialized(), Is.False);
        }

        [Test]
        public void RegisteringASerializerOnTwoChannelsShouldRaiseAnException()
        {
            var action = new TestDelegate(() =>
                {
                    manager.Register(serializerA, channelA);
                    manager.Register(serializerA, channelB);
                }
            );

            Assert.Throws(
                Is.InstanceOf<PersistentDataSerializerAlreadyRegisterException>(),
                action);

            // TODO: Shoud I not assert that serializerA is not registered in channelB?
        }
    }
}