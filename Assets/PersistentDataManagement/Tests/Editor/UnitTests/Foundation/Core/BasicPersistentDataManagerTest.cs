
using Arman.Foundation.Core.PersistentDataManagement;
using Arman.Utility.Core;
using NUnit.Framework;


namespace Arman.Tests.Foundation.Core.PersistentDataManagement
{

    public class FakeSerializer : PersistentDataSerializer
    {
        int serializedCalls;


        public bool IsSerializedCalledOnce()
        {
            return serializedCalls == 1;
        }

        public void Serialize()
        {
            serializedCalls++;
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

        //[Test]
        //public void SaveAllShouldGivePersistentDataInterfaceToSerializers()
        //{
        //    manager.SetPersistentDataHandler()
        //    manager.Register(serializerA);
        //    manager.Register(serializerB);

        //    manager.SaveAll();

        //    Assert.That(serializerA.IsSerializedCalledOnce(), Is.True);
        //    Assert.That(serializerB.IsSerializedCalledOnce(), Is.True);
        //}
    }
}