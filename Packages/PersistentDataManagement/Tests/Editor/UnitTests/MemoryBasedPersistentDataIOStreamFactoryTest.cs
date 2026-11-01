using System.IO;
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class MemoryBasedPersistentDataIOStreamFactoryTest
    {
        MemoryBasedPersistentDataIOStreamFactory _streamFactory = null!;

        IChannel _channel1 = null!;
        IChannel _channel2 = null!;

        [SetUp]
        public void Setup()
        {
            _streamFactory = new MemoryBasedPersistentDataIOStreamFactory();

            _channel1 = new NamedChannel("Channel1");
            _channel2 = new NamedChannel("Channel2");
        }

        [Test]
        public void ReadingAChannelShouldReturnWhatWasWrittenToIt()
        {
            Write(_channel1, "content");

            Assert.That(Read(_channel1), Is.EqualTo("content"));
        }

        [Test]
        public void ReadingAChannelThatWasNeverWrittenShouldReturnNothing()
        {
            Assert.That(Read(_channel1), Is.Empty);
        }

        [Test]
        public void EveryChannelShouldBeReadableEvenBeforeItIsWritten()
        {
            Assert.That(_streamFactory.HasReadableStreamFor(_channel1), Is.True);
        }

        [Test]
        public void WritingAChannelAgainShouldReplaceItsContent()
        {
            Write(_channel1, "first");
            Write(_channel1, "second");

            Assert.That(Read(_channel1), Is.EqualTo("second"));
        }

        [Test]
        public void ChannelsShouldNotShareTheirContent()
        {
            Write(_channel1, "content1");
            Write(_channel2, "content2");

            Assert.That(Read(_channel1), Is.EqualTo("content1"));
            Assert.That(Read(_channel2), Is.EqualTo("content2"));
        }

        [Test]
        public void ReadingAChannelTwiceShouldReturnTheSameContent()
        {
            Write(_channel1, "content");

            Read(_channel1);

            Assert.That(Read(_channel1), Is.EqualTo("content"));
        }

        [Test]
        public void DeletingAChannelShouldRemoveItsContent()
        {
            Write(_channel1, "content1");
            Write(_channel2, "content2");

            _streamFactory.Delete(_channel1);

            Assert.That(Read(_channel1), Is.Empty);
            Assert.That(Read(_channel2), Is.EqualTo("content2"));
        }

        [Test]
        public void DeletingAChannelThatWasNeverWrittenShouldNotThrow()
        {
            Assert.That(() => _streamFactory.Delete(_channel1), Throws.Nothing);
        }

        void Write(IChannel channel, string content)
        {
            using (var writer = _streamFactory.CreateWriteStreamFor(channel))
                writer.Write(content);
        }

        string Read(IChannel channel)
        {
            using (var reader = _streamFactory.CreateReadStreamFor(channel))
                return reader.ReadToEnd();
        }
    }
}
