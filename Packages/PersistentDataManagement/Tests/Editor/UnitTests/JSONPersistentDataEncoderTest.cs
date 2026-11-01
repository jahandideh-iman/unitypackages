using System.IO;
using System.Text;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class JSONPersistentDataEncoderTest
    {
        JSONPersistentDataEncoder _dataEncoder;

        [SetUp]
        public void Setup()
        {
            _dataEncoder = new JSONPersistentDataEncoder();
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
        public void ClearShouldRemoveAllValues()
        {
            _dataEncoder.WriteString("Key1", "value");
            _dataEncoder.WriteInt("Key2", 2);
            _dataEncoder.WriteFloat("Key3", 3f);
            _dataEncoder.WriteBoolean("Key4", false);

            _dataEncoder.Clear();

            Assert.That(_dataEncoder.HasKey("Key1"), Is.False);
            Assert.That(_dataEncoder.HasKey("Key2"), Is.False);
            Assert.That(_dataEncoder.HasKey("Key3"), Is.False);
            Assert.That(_dataEncoder.HasKey("Key4"), Is.False);
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

        [Test]
        public void ReadFromAnEmptyStreamShouldLeaveTheEncoderEmpty()
        {
            _dataEncoder.ReadFrom(ReaderOver(""));

            Assert.That(_dataEncoder.HasKey("Key"), Is.False);
            Assert.That(_dataEncoder.ReadInt("Key", 7), Is.EqualTo(7));
        }

        [Test]
        public void ReadFromAWhitespaceOnlyStreamShouldLeaveTheEncoderEmpty()
        {
            _dataEncoder.ReadFrom(ReaderOver(" \r\n\t "));

            Assert.That(_dataEncoder.HasKey("Key"), Is.False);
            Assert.That(_dataEncoder.ReadString("Key", "default"), Is.EqualTo("default"));
        }

        [Test]
        public void WritingAfterReadingFromAnEmptyStreamShouldStillWork()
        {
            _dataEncoder.ReadFrom(ReaderOver(""));

            _dataEncoder.WriteInt("Key", 5);

            Assert.That(_dataEncoder.ReadInt("Key"), Is.EqualTo(5));
        }

        [Test]
        public void ReadingAnAbsentBlockShouldYieldDefaults()
        {
            _dataEncoder.WriteInt("outerKey", 1);

            _dataEncoder.BeginReadingBlock("absent");

            Assert.That(_dataEncoder.HasKey("outerKey"), Is.False);
            Assert.That(_dataEncoder.ReadInt("anything", 9), Is.EqualTo(9));
        }

        [Test]
        public void EndingAnAbsentBlockShouldRestoreTheEnclosingBlock()
        {
            _dataEncoder.WriteInt("outerKey", 1);

            _dataEncoder.BeginReadingBlock("absent");
            _dataEncoder.EndReadingBlock();

            Assert.That(_dataEncoder.ReadInt("outerKey"), Is.EqualTo(1));
        }

        [Test]
        public void BlocksNestedInsideAnAbsentBlockShouldAlsoBeAbsent()
        {
            _dataEncoder.WriteInt("outerKey", 1);

            _dataEncoder.BeginReadingBlock("absent");
            _dataEncoder.BeginReadingBlock("alsoAbsent");
            Assert.That(_dataEncoder.ReadInt("anything", 9), Is.EqualTo(9));
            _dataEncoder.EndReadingBlock();
            _dataEncoder.EndReadingBlock();

            Assert.That(_dataEncoder.ReadInt("outerKey"), Is.EqualTo(1));
        }

        [Test]
        public void ReadingMissingKeysShouldReturnTheGivenDefaults()
        {
            Assert.That(_dataEncoder.HasKey("Missing"), Is.False);
            Assert.That(_dataEncoder.ReadInt("Missing", 3), Is.EqualTo(3));
            Assert.That(_dataEncoder.ReadFloat("Missing", 2.5f), Is.EqualTo(2.5f));
            Assert.That(_dataEncoder.ReadString("Missing", "default"), Is.EqualTo("default"));
        }

        [Test]
        public void ReadingMissingKeysWithoutDefaultsShouldReturnTheTypeDefaults()
        {
            Assert.That(_dataEncoder.ReadInt("Missing"), Is.EqualTo(0));
            Assert.That(_dataEncoder.ReadFloat("Missing"), Is.EqualTo(0f));
            Assert.That(_dataEncoder.ReadBoolean("Missing"), Is.False);
            Assert.That(_dataEncoder.ReadString("Missing"), Is.EqualTo(""));
        }

        [Test]
        public void AWrittenValueShouldWinOverTheDefault()
        {
            _dataEncoder.WriteInt("Key", 0);

            Assert.That(_dataEncoder.ReadInt("Key", 9), Is.EqualTo(0));
        }

        [Test]
        public void KeysWrittenInsideABlockShouldNotBeVisibleOutsideIt()
        {
            _dataEncoder.BeginWritingBlock("block").WriteInt("innerKey", 1).EndWritingBlock();

            Assert.That(_dataEncoder.HasKey("block"), Is.True);
            Assert.That(_dataEncoder.HasKey("innerKey"), Is.False);
        }

        [Test]
        public void ReadFromStreamShouldRestoreNestedBlocks()
        {
            _dataEncoder
                .BeginWritingBlock("outer")
                .WriteInt("outerKey", 1)
                .BeginWritingBlock("inner")
                .WriteString("innerKey", "innerValue")
                .EndWritingBlock()
                .EndWritingBlock();

            var reloaded = new JSONPersistentDataEncoder();
            reloaded.ReadFrom(ReaderOver(WrittenContent(_dataEncoder)));

            reloaded.BeginReadingBlock("outer");
            Assert.That(reloaded.ReadInt("outerKey"), Is.EqualTo(1));
            reloaded.BeginReadingBlock("inner");
            Assert.That(reloaded.ReadString("innerKey"), Is.EqualTo("innerValue"));
            reloaded.EndReadingBlock();
            reloaded.EndReadingBlock();
        }

        [Test]
        public void ReadFromStreamShouldReplaceThePreviousContent()
        {
            var other = new JSONPersistentDataEncoder();
            other.WriteInt("OtherKey", 2);

            _dataEncoder.WriteInt("Key", 1);
            _dataEncoder.ReadFrom(ReaderOver(WrittenContent(other)));

            Assert.That(_dataEncoder.HasKey("Key"), Is.False);
            Assert.That(_dataEncoder.ReadInt("OtherKey"), Is.EqualTo(2));
        }

        [Test]
        public void ClearingInsideABlockShouldReturnWritingToTheRoot()
        {
            _dataEncoder.BeginWritingBlock("block");

            _dataEncoder.Clear();
            _dataEncoder.WriteInt("Key", 1);

            Assert.That(_dataEncoder.ReadInt("Key"), Is.EqualTo(1));
            Assert.That(_dataEncoder.HasKey("block"), Is.False);
        }

        [Test]
        public void WritingStringsWithSpecialCharactersShouldSurviveAStreamRoundTrip()
        {
            const string value = "quote \" backslash \\ newline \n tab \t";
            _dataEncoder.WriteString("Key", value);

            var reloaded = new JSONPersistentDataEncoder();
            reloaded.ReadFrom(ReaderOver(WrittenContent(_dataEncoder)));

            Assert.That(reloaded.ReadString("Key"), Is.EqualTo(value));
        }

        private static string WrittenContent(JSONPersistentDataEncoder dataEncoder)
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream))
            {
                dataEncoder.WriteTo(streamWriter);
                streamWriter.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static StreamReader ReaderOver(string content)
        {
            return new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
    }
}
