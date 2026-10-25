using System.Collections.Generic;
using Arman.PackageBasics;
using Moq;
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
            manager = new UpdateManager();
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

        [Test]
        public void AnUpdatableThatPausesItsOwnChannelShouldStopTheRestOfTheChannelFromUpdating()
        {
            var updateManager = new UpdateManager();

            // Updatables are walked back to front, so the pauser (registered last) runs first.
            var skipped = new Mock<IUpdatable>();
            var pauser = new Mock<IUpdatable>();
            pauser
                .Setup(updatable => updatable.UpdateTime(It.IsAny<float>()))
                .Callback(() => updateManager.Pause(channel));

            updateManager.RegisterUpdatable(skipped.Object, channel);
            updateManager.RegisterUpdatable(pauser.Object, channel);

            updateManager.AdvanceTime(1f);

            skipped.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Never);
        }

        void Record(IChannel channel, bool isPaused)
        {
            raised.Add((channel, isPaused));
        }
    }
}
