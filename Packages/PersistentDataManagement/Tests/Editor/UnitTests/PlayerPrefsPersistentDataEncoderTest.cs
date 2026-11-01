using System.IO;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PlayerPrefsPersistentDataEncoderTest
    {
        PlayerPrefsPersistentDataEncoder _dataEncoder;

        [SetUp]
        public void Setup()
        {
            _dataEncoder = new PlayerPrefsPersistentDataEncoder();
        }

        [Test]
        public void ReadIntShouldReadAWrittenInt()
        {
            _dataEncoder.WriteInt("Key", 5);

            Assert.That(_dataEncoder.ReadInt("Key"), Is.EqualTo(5));
            Assert.That(_dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadFloatShouldReadAWrittenFloat()
        {
            _dataEncoder.WriteFloat("Key", 5f);

            Assert.That(_dataEncoder.ReadFloat("Key"), Is.EqualTo(5f));
            Assert.That(_dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadBooleanShouldReadAWrittenBoolean()
        {
            _dataEncoder.WriteBoolean("Key", true);

            Assert.That(_dataEncoder.ReadBoolean("Key"), Is.EqualTo(true));
            Assert.That(_dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadStringShouldReadAWrittenString()
        {
            _dataEncoder.WriteString("Key", "value");

            Assert.That(_dataEncoder.ReadString("Key"), Is.EqualTo("value"));
            Assert.That(_dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadBlockShouldReadAWrittenBlock()
        {
            _dataEncoder
                .BeginWritingBlock("outerblock1")
                .WriteInt("key1", 1)
                .WriteFloat("key2", 1f)
                .BeginWritingBlock("innerblock")
                .WriteString("innerKey1", "innerValue1")
                .EndWritingBlock()
                .EndWritingBlock();

            _dataEncoder
                .BeginWritingBlock("outerblock2")
                .WriteInt("key1", 2)
                .WriteFloat("key2", 2f)
                .BeginWritingBlock("innerblock")
                .WriteString("innerKey2", "innerValue2")
                .EndWritingBlock()
                .EndWritingBlock();

            _dataEncoder.BeginReadingBlock("outerblock1");
            Assert.That(_dataEncoder.ReadInt("key1"), Is.EqualTo(1));
            Assert.That(_dataEncoder.ReadFloat("key2"), Is.EqualTo(1f));
            _dataEncoder.BeginReadingBlock("innerblock");
            Assert.That(_dataEncoder.ReadString("innerKey1"), Is.EqualTo("innerValue1"));
            _dataEncoder.EndReadingBlock();
            _dataEncoder.EndReadingBlock();

            _dataEncoder.BeginReadingBlock("outerblock2");
            Assert.That(_dataEncoder.ReadInt("key1"), Is.EqualTo(2));
            Assert.That(_dataEncoder.ReadFloat("key2"), Is.EqualTo(2f));
            _dataEncoder.BeginReadingBlock("innerblock");
            Assert.That(_dataEncoder.ReadString("innerKey2"), Is.EqualTo("innerValue2"));
            _dataEncoder.EndReadingBlock();
            _dataEncoder.EndReadingBlock();
        }

        [Test]
        public void ReadFromStreamShouldReadFromWrittenStream()
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream))
            {
                _dataEncoder.WriteString("Key1", "value");
                _dataEncoder.WriteInt("Key2", 2);
                _dataEncoder.WriteFloat("Key3", 3f);
                _dataEncoder.WriteBoolean("Key4", false);

                _dataEncoder.WriteTo(streamWriter);
                streamWriter.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                _dataEncoder.Clear();
                _dataEncoder.ReadFrom(new StreamReader(stream));

                Assert.That(_dataEncoder.ReadString("Key1"), Is.EqualTo("value"));
                Assert.That(_dataEncoder.ReadInt("Key2"), Is.EqualTo(2));
                Assert.That(_dataEncoder.ReadFloat("Key3"), Is.EqualTo(3f));
                Assert.That(_dataEncoder.ReadBoolean("Key4"), Is.EqualTo(false));
            }
        }
    }
}
