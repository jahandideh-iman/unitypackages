using System;
using System.IO;
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.PersistentDataManagement.Tests
{
    public class FileBasedPersistentDataIOStreamFactoryTest
    {
        string _directory = null!;
        FileBasedPersistentDataIOStreamFactory _streamFactory = null!;

        IChannel _channel1 = null!;
        IChannel _channel2 = null!;

        [SetUp]
        public void Setup()
        {
            // A directory that does not exist yet: the factory has to create it on its first write.
            _directory = Path.Combine(
                Path.GetTempPath(),
                "FileBasedPersistentDataIOStreamFactoryTest",
                Guid.NewGuid().ToString("N")
            );
            _streamFactory = new FileBasedPersistentDataIOStreamFactory(_directory);

            _channel1 = new NamedChannel("Channel1");
            _channel2 = new NamedChannel("Channel2");
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void AChannelThatWasNeverWrittenShouldNotBeReadable()
        {
            Assert.That(_streamFactory.HasReadableStreamFor(_channel1), Is.False);
        }

        [Test]
        public void AWrittenChannelShouldBeReadable()
        {
            Write(_channel1, "content");

            Assert.That(_streamFactory.HasReadableStreamFor(_channel1), Is.True);
            Assert.That(_streamFactory.HasReadableStreamFor(_channel2), Is.False);
        }

        [Test]
        public void ReadingAChannelShouldReturnWhatWasWrittenToIt()
        {
            Write(_channel1, "content");

            Assert.That(Read(_channel1), Is.EqualTo("content"));
        }

        [Test]
        public void WritingAChannelShouldStoreItInAFileNamedAfterTheChannelUnderThePath()
        {
            Write(_channel1, "content");

            var filePath = Path.Combine(_directory, _channel1.ToString());
            Assert.That(File.ReadAllText(filePath), Is.EqualTo("content"));
        }

        [Test]
        public void WritingAChannelAgainShouldReplaceItsContent()
        {
            Write(_channel1, "a longer first content");
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
        public void DeletingAChannelShouldMakeItUnreadable()
        {
            Write(_channel1, "content1");
            Write(_channel2, "content2");

            _streamFactory.Delete(_channel1);

            Assert.That(_streamFactory.HasReadableStreamFor(_channel1), Is.False);
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
