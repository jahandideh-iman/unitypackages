using System.Collections.Generic;
using NUnit.Framework;

namespace Arman.PackageBasics.Tests
{
    public class ChannelTest
    {
        [Test]
        public void NamedChannelsWithTheSameNameShouldBeEqual()
        {
            var channel = new NamedChannel("Channel");
            var other = new NamedChannel("Channel");

            Assert.That(channel.Equals(other), Is.True);
            Assert.That(channel.GetHashCode(), Is.EqualTo(other.GetHashCode()));
        }

        [Test]
        public void NamedChannelsWithDifferentNamesShouldNotBeEqual()
        {
            Assert.That(new NamedChannel("A").Equals(new NamedChannel("B")), Is.False);
        }

        [Test]
        public void ANamedChannelShouldNotEqualNullOrAnotherKindOfChannel()
        {
            var channel = new NamedChannel("1");

            Assert.That(channel.Equals(null), Is.False);
            Assert.That(channel.Equals(new IDedChannel(1)), Is.False);
            Assert.That(channel.Equals("1"), Is.False);
        }

        [Test]
        public void ANamedChannelShouldPrintAsItsName()
        {
            Assert.That(new NamedChannel("Channel").ToString(), Is.EqualTo("Channel"));
        }

        [Test]
        public void IDedChannelsWithTheSameIdShouldBeEqual()
        {
            var channel = new IDedChannel(7);
            var other = new IDedChannel(7);

            Assert.That(channel.Equals(other), Is.True);
            Assert.That(channel.GetHashCode(), Is.EqualTo(other.GetHashCode()));
        }

        [Test]
        public void IDedChannelsWithDifferentIdsShouldNotBeEqual()
        {
            Assert.That(new IDedChannel(1).Equals(new IDedChannel(2)), Is.False);
        }

        [Test]
        public void AnIDedChannelShouldNotEqualNullOrAnotherKindOfChannel()
        {
            var channel = new IDedChannel(1);

            Assert.That(channel.Equals(null), Is.False);
            Assert.That(channel.Equals(new NamedChannel("1")), Is.False);
            Assert.That(channel.Equals(1), Is.False);
        }

        [Test]
        public void AnIDedChannelShouldExposeAndPrintItsId()
        {
            var channel = new IDedChannel(42);

            Assert.That(channel.Id, Is.EqualTo(42));
            Assert.That(channel.ToString(), Is.EqualTo("42"));
        }

        [Test]
        public void EqualChannelsShouldFindTheSameDictionaryEntry()
        {
            var values = new Dictionary<IChannel, string>
            {
                [new NamedChannel("Named")] = "named",
                [new IDedChannel(1)] = "ided",
            };

            Assert.That(values[new NamedChannel("Named")], Is.EqualTo("named"));
            Assert.That(values[new IDedChannel(1)], Is.EqualTo("ided"));
        }
    }
}
