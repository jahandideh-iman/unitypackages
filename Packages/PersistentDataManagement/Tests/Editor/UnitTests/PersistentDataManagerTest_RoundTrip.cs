using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    // Saves and loads through the real JSON encoder and an in-memory stream factory, and
    // asserts on the values that come back rather than on the calls that carried them.
    [TestFixture]
    public class PersistentDataManagerTest_RoundTrip
    {
        class FakeSerializer : IPersistentDataSerializer
        {
            readonly string _key;

            public int Number;
            public float Ratio;
            public bool Flag;
            public string Text = "";

            public FakeSerializer(string key)
            {
                _key = key;
            }

            public string Key()
            {
                return _key;
            }

            public void SerializeTo(IWritablePersistentDataEncoder persistentDataEncoder)
            {
                persistentDataEncoder
                    .WriteInt("Number", Number)
                    .WriteFloat("Ratio", Ratio)
                    .WriteBoolean("Flag", Flag)
                    .WriteString("Text", Text);
            }

            public void DeserializeFrom(IReadablePersistentDataEncoder persistentDataEncoder)
            {
                Number = persistentDataEncoder.ReadInt("Number");
                Ratio = persistentDataEncoder.ReadFloat("Ratio");
                Flag = persistentDataEncoder.ReadBoolean("Flag");
                Text = persistentDataEncoder.ReadString("Text");
            }

            public void Set(int number, float ratio, bool flag, string text)
            {
                Number = number;
                Ratio = ratio;
                Flag = flag;
                Text = text;
            }
        }

        const int _saveVersion = 3;

        MemoryBasedPersistentDataIOStreamFactory _streamFactory = null!;
        PersistentDataManager _manager = null!;

        FakeSerializer _serializerA = null!;
        FakeSerializer _serializerB = null!;

        IChannel _channel1 = null!;
        IChannel _channel2 = null!;

        [SetUp]
        public void Setup()
        {
            _streamFactory = new MemoryBasedPersistentDataIOStreamFactory();
            _manager = new PersistentDataManager(
                _streamFactory,
                new JSONPersistentDataEncoder(),
                _saveVersion
            );

            _serializerA = new FakeSerializer("A");
            _serializerB = new FakeSerializer("B");

            _channel1 = new NamedChannel("Channel1");
            _channel2 = new NamedChannel("Channel2");
        }

        [Test]
        public void LoadingShouldRestoreTheSavedValues()
        {
            _manager.Register(_serializerA, _channel1);
            _serializerA.Set(7, 0.5f, true, "saved");

            _manager.Save(_channel1);
            _serializerA.Set(0, 0f, false, "changed");
            _manager.Load(_channel1);

            Assert.That(_serializerA.Number, Is.EqualTo(7));
            Assert.That(_serializerA.Ratio, Is.EqualTo(0.5f));
            Assert.That(_serializerA.Flag, Is.True);
            Assert.That(_serializerA.Text, Is.EqualTo("saved"));
        }

        [Test]
        public void LoadingAllShouldRestoreTheSavedValuesOfEveryChannel()
        {
            _manager.Register(_serializerA, _channel1);
            _manager.Register(_serializerB, _channel2);
            _serializerA.Set(1, 1f, true, "a");
            _serializerB.Set(2, 2f, false, "b");

            _manager.SaveAll();
            _serializerA.Set(0, 0f, false, "");
            _serializerB.Set(0, 0f, true, "");
            _manager.LoadAll();

            Assert.That(_serializerA.Number, Is.EqualTo(1));
            Assert.That(_serializerA.Text, Is.EqualTo("a"));
            Assert.That(_serializerB.Number, Is.EqualTo(2));
            Assert.That(_serializerB.Flag, Is.False);
            Assert.That(_serializerB.Text, Is.EqualTo("b"));
        }

        [Test]
        public void SerializersOnTheSameChannelShouldKeepTheirValuesApart()
        {
            _manager.Register(_serializerA, _channel1);
            _manager.Register(_serializerB, _channel1);
            _serializerA.Set(1, 0f, false, "a");
            _serializerB.Set(2, 0f, false, "b");

            _manager.Save(_channel1);
            _serializerA.Set(0, 0f, false, "");
            _serializerB.Set(0, 0f, false, "");
            _manager.Load(_channel1);

            Assert.That(_serializerA.Number, Is.EqualTo(1));
            Assert.That(_serializerA.Text, Is.EqualTo("a"));
            Assert.That(_serializerB.Number, Is.EqualTo(2));
            Assert.That(_serializerB.Text, Is.EqualTo("b"));
        }

        [Test]
        public void SerializersRegisteredWithoutAChannelShouldBeSavedAndLoaded()
        {
            _manager.Register(_serializerA);
            _serializerA.Set(5, 0f, false, "default channel");

            _manager.SaveAll();
            _serializerA.Set(0, 0f, false, "");
            _manager.LoadAll();

            Assert.That(_serializerA.Number, Is.EqualTo(5));
            Assert.That(_serializerA.Text, Is.EqualTo("default channel"));
        }

        [Test]
        public void SavingAChannelShouldNotChangeWhatAnotherChannelLoads()
        {
            _manager.Register(_serializerA, _channel1);
            _manager.Register(_serializerB, _channel2);
            _serializerB.Set(2, 0f, false, "first save");
            _manager.Save(_channel2);

            _serializerA.Set(1, 0f, false, "a");
            _serializerB.Set(9, 0f, false, "not saved");
            _manager.Save(_channel1);
            _manager.Load(_channel2);

            Assert.That(_serializerB.Number, Is.EqualTo(2));
            Assert.That(_serializerB.Text, Is.EqualTo("first save"));
        }

        [Test]
        public void LoadingAChannelThatWasNeverSavedShouldLeaveTheValuesUnchanged()
        {
            _manager.Register(_serializerA, _channel1);
            _serializerA.Set(4, 0f, false, "unchanged");

            _manager.Load(_channel1);

            Assert.That(_serializerA.Number, Is.EqualTo(4));
            Assert.That(_serializerA.Text, Is.EqualTo("unchanged"));
        }

        [Test]
        public void LoadingADeletedChannelShouldLeaveTheValuesUnchanged()
        {
            _manager.Register(_serializerA, _channel1);
            _serializerA.Set(1, 0f, false, "saved");
            _manager.Save(_channel1);

            _manager.Delete(_channel1);
            _serializerA.Set(2, 0f, false, "after delete");
            _manager.Load(_channel1);

            Assert.That(_serializerA.Number, Is.EqualTo(2));
            Assert.That(_serializerA.Text, Is.EqualTo("after delete"));
        }

        [Test]
        public void SavingShouldWriteTheSaveVersionAsMetaData()
        {
            _manager.Register(_serializerA, _channel1);

            _manager.Save(_channel1);

            var dataEncoder = new JSONPersistentDataEncoder();
            using (var readStream = _streamFactory.CreateReadStreamFor(_channel1))
                dataEncoder.ReadFrom(readStream);

            dataEncoder.BeginReadingBlock("MetaData");
            Assert.That(dataEncoder.ReadInt("Version"), Is.EqualTo(_saveVersion));
            dataEncoder.EndReadingBlock();
        }

        [Test]
        public void SavingShouldWriteEachSerializerUnderItsKeyInTheDataBlock()
        {
            _manager.Register(_serializerA, _channel1);
            _serializerA.Set(6, 0f, false, "under its key");

            _manager.Save(_channel1);

            var dataEncoder = new JSONPersistentDataEncoder();
            using (var readStream = _streamFactory.CreateReadStreamFor(_channel1))
                dataEncoder.ReadFrom(readStream);

            dataEncoder.BeginReadingBlock("Data");
            Assert.That(dataEncoder.HasKey(_serializerA.Key()), Is.True);
            dataEncoder.BeginReadingBlock(_serializerA.Key());
            Assert.That(dataEncoder.ReadInt("Number"), Is.EqualTo(6));
            Assert.That(dataEncoder.ReadString("Text"), Is.EqualTo("under its key"));
        }
    }
}
