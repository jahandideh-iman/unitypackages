using System.Collections.Generic;
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class UpdateManagerTest_ChannelStateChangedEvent
    {
        IUpdateManager manager = null!;
        IChannel channel = null!;

        List<(IChannel channel, bool isPaused)> raised = null!;

        [SetUp]
        public void Setup()
        {
            manager = new BasicUpdateManager();
            channel = new NamedChannel("Channel");

            raised = new List<(IChannel channel, bool isPaused)>();
        }

        [Test]
        public void PausingARegisteredChannelShouldRaiseTheEvent()
        {
            manager.RegisterChannel(channel);
            manager.ChannelStateChangedEvent += Record;

            manager.Pause(channel);

            Assert.That(raised, Is.EqualTo(new[] { (channel, true) }));
        }

        [Test]
        public void ResumingARegisteredChannelShouldRaiseTheEvent()
        {
            manager.RegisterChannel(channel);
            manager.Pause(channel);

            manager.ChannelStateChangedEvent += Record;

            manager.Resume(channel);

            Assert.That(raised, Is.EqualTo(new[] { (channel, false) }));
        }

        [Test]
        public void PausingAnUnregisteredChannelShouldNotRaiseTheEvent()
        {
            manager.ChannelStateChangedEvent += Record;

            manager.Pause(new NamedChannel("Unregistered"));

            Assert.That(raised, Is.Empty);
        }

        [Test]
        public void AnUnsubscribedHandlerShouldNotBeCalled()
        {
            manager.RegisterChannel(channel);

            manager.ChannelStateChangedEvent += Record;
            manager.ChannelStateChangedEvent -= Record;

            manager.Pause(channel);

            Assert.That(raised, Is.Empty);
        }

        void Record(IChannel channel, bool isPaused)
        {
            raised.Add((channel, isPaused));
        }
    }
}
