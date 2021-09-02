using Arman.Foundation.Unity.PersistentDataManagement;
using NUnit.Framework;
using System.IO;

namespace Arman.Tests.Foundation.Unity.PersistentDataManagement
{
    public class JSONPersistentDataWrapperTest 
    {
        JSONPersistentDataWrapper dataWrapper;

        [SetUp]
        public void Setup()
        {
            dataWrapper = new JSONPersistentDataWrapper();
        }

        [Test]
        public void ReadIntShoudReadAWrittenInt()
        {
            dataWrapper.WriteInt("Key", 5);

            Assert.That(dataWrapper.ReadInt("Key"), Is.EqualTo(5));
            Assert.That(dataWrapper.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadFloatShoudReadAWrittenFloat()
        {
            dataWrapper.WriteFloat("Key", 5f);

            Assert.That(dataWrapper.ReadFloat("Key"), Is.EqualTo(5f));
            Assert.That(dataWrapper.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadBooleanShoudReadAWrittenBoolean()
        {
            dataWrapper.WriteBoolean("Key", true);

            Assert.That(dataWrapper.ReadBoolean("Key"), Is.EqualTo(true));
            Assert.That(dataWrapper.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadStringShoudReadAWrittenString()
        {
            dataWrapper.WriteString("Key", "value");

            Assert.That(dataWrapper.ReadString("Key"), Is.EqualTo("value"));
            Assert.That(dataWrapper.HasKey("Key"), Is.True);
        }

        [Test]
        public void ReadBlockShoudReadAWrittenBlock()
        {
            dataWrapper.
                BeginWritingBlock("outerblock1").
                    WriteInt("key1", 1).
                    WriteFloat("key2", 1f).
                    BeginWritingBlock("innerblock").
                        WriteString("innerKey1", "innerValue1" ).
                    EndWritingBlock().
                EndWritingBlock();

            dataWrapper.
                BeginWritingBlock("outerblock2").
                    WriteInt("key1", 2).
                    WriteFloat("key2", 2f).
                    BeginWritingBlock("innerblock").
                        WriteString("innerKey2", "innerValue2").
                    EndWritingBlock().
                EndWritingBlock();


            dataWrapper.BeginReadingBlock("outerblock1");
                Assert.That(dataWrapper.ReadInt("key1"), Is.EqualTo(1));
                Assert.That(dataWrapper.ReadFloat("key2"), Is.EqualTo(1f));
                dataWrapper.BeginReadingBlock("innerblock");
                    Assert.That(dataWrapper.ReadString("innerKey1"), Is.EqualTo("innerValue1"));
                dataWrapper.EndReadingBlock();
            dataWrapper.EndReadingBlock();

            dataWrapper.BeginReadingBlock("outerblock2");
                Assert.That(dataWrapper.ReadInt("key1"), Is.EqualTo(2));
                Assert.That(dataWrapper.ReadFloat("key2"), Is.EqualTo(2f));
                dataWrapper.BeginReadingBlock("innerblock");
                    Assert.That(dataWrapper.ReadString("innerKey2"), Is.EqualTo("innerValue2"));
                dataWrapper.EndReadingBlock();
            dataWrapper.EndReadingBlock();
        }


        [Test]
        public void ClearShouldRemoveAllValues()
        {
            dataWrapper.WriteString("Key1", "value");
            dataWrapper.WriteInt("Key2", 2);
            dataWrapper.WriteFloat("Key3", 3f);
            dataWrapper.WriteBoolean("Key4", false);

            dataWrapper.Clear();

            Assert.That(dataWrapper.HasKey("Key1"), Is.False);
            Assert.That(dataWrapper.HasKey("Key2"), Is.False);
            Assert.That(dataWrapper.HasKey("Key3"), Is.False);
            Assert.That(dataWrapper.HasKey("Key4"), Is.False);
        }

        [Test]
        public void ReadFromStreamShouldReadFromWrittenStream()
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream))
            {
                dataWrapper.WriteString("Key1", "value");
                dataWrapper.WriteInt("Key2", 2);
                dataWrapper.WriteFloat("Key3", 3f);
                dataWrapper.WriteBoolean("Key4", false);

                dataWrapper.WriteTo(streamWriter);
                streamWriter.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                dataWrapper.Clear();
                dataWrapper.ReadFrom(new StreamReader(stream));

                Assert.That(dataWrapper.ReadString("Key1"), Is.EqualTo("value"));
                Assert.That(dataWrapper.ReadInt("Key2"), Is.EqualTo(2));
                Assert.That(dataWrapper.ReadFloat("Key3"), Is.EqualTo(3f));
                Assert.That(dataWrapper.ReadBoolean("Key4"), Is.EqualTo(false));
            }

        }
    }
}