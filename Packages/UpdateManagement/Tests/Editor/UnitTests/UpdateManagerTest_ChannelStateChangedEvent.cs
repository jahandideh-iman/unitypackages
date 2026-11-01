using System.Collections.Generic;
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class UpdateManagerTest_ChannelStateChangedEvent
    {
        IUpdateManager _manager = null!;
        IChannel _channel = null!;

        List<(IChannel channel, bool isPaused)> _raised = null!;

        [SetUp]
        public void Setup()
        {
            _manager = new UpdateManager();
            _channel = new NamedChannel("Channel");

            _raised = new List<(IChannel channel, bool isPaused)>();
        }

        [Test]
        public void PausingARegisteredChannelShouldRaiseTheEvent()
        {
            _manager.RegisterChannel(_channel);
            _manager.ChannelStateChangedEvent += Record;

            _manager.Pause(_channel);

            Assert.That(_raised, Is.EqualTo(new[] { (_channel, true) }));
        }

        [Test]
        public void ResumingARegisteredChannelShouldRaiseTheEvent()
        {
            _manager.RegisterChannel(_channel);
            _manager.Pause(_channel);

            _manager.ChannelStateChangedEvent += Record;

            _manager.Resume(_channel);

            Assert.That(_raised, Is.EqualTo(new[] { (_channel, false) }));
        }

        [Test]
        public void PausingAnUnregisteredChannelShouldNotRaiseTheEvent()
        {
            _manager.ChannelStateChangedEvent += Record;

            _manager.Pause(new NamedChannel("Unregistered"));

            Assert.That(_raised, Is.Empty);
        }

        [Test]
        public void AnUnsubscribedHandlerShouldNotBeCalled()
        {
            _manager.RegisterChannel(_channel);

            _manager.ChannelStateChangedEvent += Record;
            _manager.ChannelStateChangedEvent -= Record;

            _manager.Pause(_channel);

            Assert.That(_raised, Is.Empty);
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
                .Callback(() => updateManager.Pause(_channel));

            updateManager.RegisterUpdatable(skipped.Object, _channel);
            updateManager.RegisterUpdatable(pauser.Object, _channel);

            updateManager.AdvanceTime(1f);

            skipped.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Never);
        }

        void Record(IChannel channel, bool isPaused)
        {
            _raised.Add((channel, isPaused));
        }
    }
}
