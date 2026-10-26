using System.IO;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class PlayerPrefsPersistentDataEncoderTest
    {
        PlayerPrefsPersistentDataEncoder dataEncoder;

        [SetUp]
        public void Setup()
        {
            dataEncoder = new PlayerPrefsPersistentDataEncoder();
        }

        [Test]
        public void ReadIntShouldReadAWrittenInt()
        {
            dataEncoder.WriteInt("Key", 5);

            Assert.That(dataEncoder.ReadInt("Key"), Is.EqualTo(5));
            Assert.That(dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadFloatShouldReadAWrittenFloat()
        {
            dataEncoder.WriteFloat("Key", 5f);

            Assert.That(dataEncoder.ReadFloat("Key"), Is.EqualTo(5f));
            Assert.That(dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadBooleanShouldReadAWrittenBoolean()
        {
            dataEncoder.WriteBoolean("Key", true);

            Assert.That(dataEncoder.ReadBoolean("Key"), Is.EqualTo(true));
            Assert.That(dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadStringShouldReadAWrittenString()
        {
            dataEncoder.WriteString("Key", "value");

            Assert.That(dataEncoder.ReadString("Key"), Is.EqualTo("value"));
            Assert.That(dataEncoder.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadBlockShouldReadAWrittenBlock()
        {
            dataEncoder
                .BeginWritingBlock("outerblock1")
                .WriteInt("key1", 1)
                .WriteFloat("key2", 1f)
                .BeginWritingBlock("innerblock")
                .WriteString("innerKey1", "innerValue1")
                .EndWritingBlock()
                .EndWritingBlock();

            dataEncoder
                .BeginWritingBlock("outerblock2")
                .WriteInt("key1", 2)
                .WriteFloat("key2", 2f)
                .BeginWritingBlock("innerblock")
                .WriteString("innerKey2", "innerValue2")
                .EndWritingBlock()
                .EndWritingBlock();

            dataEncoder.BeginReadingBlock("outerblock1");
            Assert.That(dataEncoder.ReadInt("key1"), Is.EqualTo(1));
            Assert.That(dataEncoder.ReadFloat("key2"), Is.EqualTo(1f));
            dataEncoder.BeginReadingBlock("innerblock");
            Assert.That(dataEncoder.ReadString("innerKey1"), Is.EqualTo("innerValue1"));
            dataEncoder.EndReadingBlock();
            dataEncoder.EndReadingBlock();

            dataEncoder.BeginReadingBlock("outerblock2");
            Assert.That(dataEncoder.ReadInt("key1"), Is.EqualTo(2));
            Assert.That(dataEncoder.ReadFloat("key2"), Is.EqualTo(2f));
            dataEncoder.BeginReadingBlock("innerblock");
            Assert.That(dataEncoder.ReadString("innerKey2"), Is.EqualTo("innerValue2"));
            dataEncoder.EndReadingBlock();
            dataEncoder.EndReadingBlock();
        }

        [Test]
        public void ReadFromStreamShouldReadFromWrittenStream()
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream))
            {
                dataEncoder.WriteString("Key1", "value");
                dataEncoder.WriteInt("Key2", 2);
                dataEncoder.WriteFloat("Key3", 3f);
                dataEncoder.WriteBoolean("Key4", false);

                dataEncoder.WriteTo(streamWriter);
                streamWriter.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                dataEncoder.Clear();
                dataEncoder.ReadFrom(new StreamReader(stream));

                Assert.That(dataEncoder.ReadString("Key1"), Is.EqualTo("value"));
                Assert.That(dataEncoder.ReadInt("Key2"), Is.EqualTo(2));
                Assert.That(dataEncoder.ReadFloat("Key3"), Is.EqualTo(3f));
                Assert.That(dataEncoder.ReadBoolean("Key4"), Is.EqualTo(false));
            }
        }
    }
}
