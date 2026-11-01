using System;
using System.Collections.Generic;
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class UpdateManagerTest_Updating
    {
        // Keeps the time it was given, so a test can read back whether and how it was updated.
        // OnUpdateAction lets a test act from inside an update.
        class FakeUpdatable : IUpdatable
        {
            public readonly List<float> ReceivedTimes = new List<float>();

            public Action? OnUpdateAction;

            public void UpdateTime(float dt)
            {
                ReceivedTimes.Add(dt);
                OnUpdateAction?.Invoke();
            }
        }

        UpdateManager _manager = null!;
        IChannel _channel = null!;
        IChannel _otherChannel = null!;

        [SetUp]
        public void Setup()
        {
            _manager = new UpdateManager();
            _channel = new NamedChannel("Channel");
            _otherChannel = new NamedChannel("OtherChannel");
        }

        [Test]
        public void HasTheRegisteredUpdatables()
        {
            var updatable1 = new FakeUpdatable();
            var updatable2 = new FakeUpdatable();

            _manager.RegisterUpdatable(updatable1, _channel);
            _manager.RegisterUpdatable(updatable2, _otherChannel);

            Assert.That(_manager.Has(updatable1), Is.True);
            Assert.That(_manager.Has(updatable2), Is.True);
            Assert.That(_manager.Has(new FakeUpdatable()), Is.False);
        }

        [Test]
        public void AdvancingTimeShouldGiveTheElapsedTimeToEveryRegisteredUpdatable()
        {
            var updatable1 = new FakeUpdatable();
            var updatable2 = new FakeUpdatable();

            _manager.RegisterUpdatable(updatable1, _channel);
            _manager.RegisterUpdatable(updatable2, _otherChannel);

            _manager.AdvanceTime(0.5f);

            Assert.That(updatable1.ReceivedTimes, Is.EqualTo(new[] { 0.5f }));
            Assert.That(updatable2.ReceivedTimes, Is.EqualTo(new[] { 0.5f }));
        }

        [Test]
        public void UnregisteringAnUpdatableShouldRemoveItAndStopItsUpdates()
        {
            var updatable = new FakeUpdatable();
            _manager.RegisterUpdatable(updatable, _channel);

            _manager.UnRegisterUpdatable(updatable);
            _manager.AdvanceTime(1f);

            Assert.That(_manager.Has(updatable), Is.False);
            Assert.That(updatable.ReceivedTimes, Is.Empty);
        }

        [Test]
        public void UnregisteringAnUnknownUpdatableShouldNotThrow()
        {
            Assert.That(() => _manager.UnRegisterUpdatable(new FakeUpdatable()), Throws.Nothing);
        }

        [Test]
        public void AnUpdatableThatUnregistersItselfShouldNotStopTheRestOfTheChannelFromUpdating()
        {
            var other = new FakeUpdatable();
            var selfRemoving = new FakeUpdatable();
            selfRemoving.OnUpdateAction = () => _manager.UnRegisterUpdatable(selfRemoving);

            _manager.RegisterUpdatable(other, _channel);
            _manager.RegisterUpdatable(selfRemoving, _channel);

            _manager.AdvanceTime(1f);
            _manager.AdvanceTime(1f);

            Assert.That(selfRemoving.ReceivedTimes, Has.Count.EqualTo(1));
            Assert.That(other.ReceivedTimes, Has.Count.EqualTo(2));
        }

        [Test]
        public void APausedChannelShouldNotBeUpdated()
        {
            var paused = new FakeUpdatable();
            var running = new FakeUpdatable();
            _manager.RegisterUpdatable(paused, _channel);
            _manager.RegisterUpdatable(running, _otherChannel);

            _manager.Pause(_channel);
            _manager.AdvanceTime(1f);

            Assert.That(paused.ReceivedTimes, Is.Empty);
            Assert.That(running.ReceivedTimes, Has.Count.EqualTo(1));
        }

        [Test]
        public void AResumedChannelShouldBeUpdatedAgain()
        {
            var updatable = new FakeUpdatable();
            _manager.RegisterUpdatable(updatable, _channel);

            _manager.Pause(_channel);
            _manager.AdvanceTime(1f);
            _manager.Resume(_channel);
            _manager.AdvanceTime(2f);

            Assert.That(updatable.ReceivedTimes, Is.EqualTo(new[] { 2f }));
        }

        [Test]
        public void PausingAParentChannelShouldPauseItsChildren()
        {
            var child = new NamedChannel("Child");
            var updatable = new FakeUpdatable();

            _manager.RegisterChannel(_channel);
            _manager.RegisterChannelToParent(child, _channel);
            _manager.RegisterUpdatable(updatable, child);

            _manager.Pause(_channel);
            _manager.AdvanceTime(1f);

            Assert.That(_manager.IsChannelGloballyPaused(child), Is.True);
            Assert.That(updatable.ReceivedTimes, Is.Empty);
        }

        [Test]
        public void PausingAGrandparentChannelShouldPauseItsGrandchildren()
        {
            var child = new NamedChannel("Child");
            var grandchild = new NamedChannel("Grandchild");

            _manager.RegisterChannel(_channel);
            _manager.RegisterChannelToParent(child, _channel);
            _manager.RegisterChannelToParent(grandchild, child);

            _manager.Pause(_channel);

            Assert.That(_manager.IsChannelGloballyPaused(grandchild), Is.True);
        }

        [Test]
        public void ResumingAParentChannelShouldResumeItsChildren()
        {
            var child = new NamedChannel("Child");
            var updatable = new FakeUpdatable();

            _manager.RegisterChannel(_channel);
            _manager.RegisterChannelToParent(child, _channel);
            _manager.RegisterUpdatable(updatable, child);

            _manager.Pause(_channel);
            _manager.Resume(_channel);
            _manager.AdvanceTime(1f);

            Assert.That(_manager.IsChannelGloballyPaused(child), Is.False);
            Assert.That(updatable.ReceivedTimes, Has.Count.EqualTo(1));
        }

        [Test]
        public void PausingAChildChannelShouldNotPauseItsParent()
        {
            var child = new NamedChannel("Child");
            var updatable = new FakeUpdatable();

            _manager.RegisterUpdatable(updatable, _channel);
            _manager.RegisterChannelToParent(child, _channel);

            _manager.Pause(child);
            _manager.AdvanceTime(1f);

            Assert.That(_manager.IsChannelGloballyPaused(_channel), Is.False);
            Assert.That(updatable.ReceivedTimes, Has.Count.EqualTo(1));
        }

        [Test]
        public void AChildChannelShouldStayPausedWhileItIsPausedItself()
        {
            var child = new NamedChannel("Child");

            _manager.RegisterChannel(_channel);
            _manager.RegisterChannelToParent(child, _channel);

            _manager.Pause(child);
            _manager.Pause(_channel);
            _manager.Resume(_channel);

            Assert.That(_manager.IsChannelGloballyPaused(child), Is.True);
        }

        [Test]
        public void ANewChannelShouldNotBePaused()
        {
            _manager.RegisterChannel(_channel);

            Assert.That(_manager.IsChannelGloballyPaused(_channel), Is.False);
        }

        [Test]
        public void ChannelsWithEqualNamesShouldBeTheSameChannel()
        {
            var updatable = new FakeUpdatable();
            _manager.RegisterUpdatable(updatable, new NamedChannel("Shared"));

            _manager.Pause(new NamedChannel("Shared"));
            _manager.AdvanceTime(1f);

            Assert.That(updatable.ReceivedTimes, Is.Empty);
        }

        [Test]
        public void RegisteringAnAlreadyRegisteredChannelShouldKeepItsUpdatablesAndState()
        {
            var updatable = new FakeUpdatable();
            _manager.RegisterUpdatable(updatable, _channel);
            _manager.Pause(_channel);

            _manager.RegisterChannel(_channel);

            Assert.That(_manager.Has(updatable), Is.True);
            Assert.That(_manager.IsChannelGloballyPaused(_channel), Is.True);
        }

        [Test]
        public void ResumingAnUnregisteredChannelShouldNotThrow()
        {
            Assert.That(() => _manager.Resume(new NamedChannel("Unregistered")), Throws.Nothing);
        }
    }
}
