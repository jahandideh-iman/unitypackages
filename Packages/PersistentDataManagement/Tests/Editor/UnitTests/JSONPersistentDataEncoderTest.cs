using System.IO;
using System.Text;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class JSONPersistentDataEncoderTest
    {
        JSONPersistentDataEncoder dataEncoder;

        [SetUp]
        public void Setup()
        {
            dataEncoder = new JSONPersistentDataEncoder();
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
        public void ClearShouldRemoveAllValues()
        {
            dataEncoder.WriteString("Key1", "value");
            dataEncoder.WriteInt("Key2", 2);
            dataEncoder.WriteFloat("Key3", 3f);
            dataEncoder.WriteBoolean("Key4", false);

            dataEncoder.Clear();

            Assert.That(dataEncoder.HasKey("Key1"), Is.False);
            Assert.That(dataEncoder.HasKey("Key2"), Is.False);
            Assert.That(dataEncoder.HasKey("Key3"), Is.False);
            Assert.That(dataEncoder.HasKey("Key4"), Is.False);
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

        [Test]
        public void ReadFromAnEmptyStreamShouldLeaveTheEncoderEmpty()
        {
            dataEncoder.ReadFrom(ReaderOver(""));

            Assert.That(dataEncoder.HasKey("Key"), Is.False);
            Assert.That(dataEncoder.ReadInt("Key", 7), Is.EqualTo(7));
        }

        [Test]
        public void ReadFromAWhitespaceOnlyStreamShouldLeaveTheEncoderEmpty()
        {
            dataEncoder.ReadFrom(ReaderOver(" \r\n\t "));

            Assert.That(dataEncoder.HasKey("Key"), Is.False);
            Assert.That(dataEncoder.ReadString("Key", "default"), Is.EqualTo("default"));
        }

        [Test]
        public void WritingAfterReadingFromAnEmptyStreamShouldStillWork()
        {
            dataEncoder.ReadFrom(ReaderOver(""));

            dataEncoder.WriteInt("Key", 5);

            Assert.That(dataEncoder.ReadInt("Key"), Is.EqualTo(5));
        }

        [Test]
        public void ReadingAnAbsentBlockShouldYieldDefaults()
        {
            dataEncoder.WriteInt("outerKey", 1);

            dataEncoder.BeginReadingBlock("absent");

            Assert.That(dataEncoder.HasKey("outerKey"), Is.False);
            Assert.That(dataEncoder.ReadInt("anything", 9), Is.EqualTo(9));
        }

        [Test]
        public void EndingAnAbsentBlockShouldRestoreTheEnclosingBlock()
        {
            dataEncoder.WriteInt("outerKey", 1);

            dataEncoder.BeginReadingBlock("absent");
            dataEncoder.EndReadingBlock();

            Assert.That(dataEncoder.ReadInt("outerKey"), Is.EqualTo(1));
        }

        [Test]
        public void BlocksNestedInsideAnAbsentBlockShouldAlsoBeAbsent()
        {
            dataEncoder.WriteInt("outerKey", 1);

            dataEncoder.BeginReadingBlock("absent");
            dataEncoder.BeginReadingBlock("alsoAbsent");
            Assert.That(dataEncoder.ReadInt("anything", 9), Is.EqualTo(9));
            dataEncoder.EndReadingBlock();
            dataEncoder.EndReadingBlock();

            Assert.That(dataEncoder.ReadInt("outerKey"), Is.EqualTo(1));
        }

        private static StreamReader ReaderOver(string content)
        {
            return new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
    }
}
