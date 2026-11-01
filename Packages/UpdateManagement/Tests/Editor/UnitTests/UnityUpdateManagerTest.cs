using System.Collections.Generic;
using Arman.PackageBasics;
using NUnit.Framework;
using UnityEngine;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class UnityUpdateManagerTest
    {
        class FakeUpdatable : IUpdatable
        {
            public void UpdateTime(float dt) { }
        }

        GameObject _gameObject = null!;
        UnityUpdateManager _manager = null!;
        IChannel _channel = null!;

        [SetUp]
        public void Setup()
        {
            _gameObject = new GameObject("UpdateManager");
            _manager = _gameObject.AddComponent<UnityUpdateManager>();
            _channel = new NamedChannel("Channel");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void HasTheRegisteredUpdatables()
        {
            var updatable = new FakeUpdatable();

            _manager.RegisterUpdatable(updatable, _channel);

            Assert.That(_manager.Has(updatable), Is.True);
        }

        [Test]
        public void UnregisteringAnUpdatableShouldRemoveIt()
        {
            var updatable = new FakeUpdatable();
            _manager.RegisterUpdatable(updatable, _channel);

            _manager.UnRegisterUpdatable(updatable);

            Assert.That(_manager.Has(updatable), Is.False);
        }

        [Test]
        public void PausingAndResumingShouldChangeTheChannelState()
        {
            _manager.RegisterChannel(_channel);

            _manager.Pause(_channel);
            Assert.That(_manager.IsChannelGloballyPaused(_channel), Is.True);

            _manager.Resume(_channel);
            Assert.That(_manager.IsChannelGloballyPaused(_channel), Is.False);
        }

        [Test]
        public void PausingAParentChannelShouldPauseItsChildren()
        {
            var child = new NamedChannel("Child");

            _manager.RegisterChannel(_channel);
            _manager.RegisterChannelToParent(child, _channel);

            _manager.Pause(_channel);

            Assert.That(_manager.IsChannelGloballyPaused(child), Is.True);
        }

        [Test]
        public void ChannelStateChangedEventShouldBeRaisedForItsSubscribers()
        {
            var raised = new List<(IChannel channel, bool isPaused)>();
            ChannelStateChanged record = (channel, isPaused) => raised.Add((channel, isPaused));
            _manager.RegisterChannel(_channel);

            _manager.ChannelStateChangedEvent += record;
            _manager.Pause(_channel);
            _manager.ChannelStateChangedEvent -= record;
            _manager.Resume(_channel);

            Assert.That(raised, Is.EqualTo(new[] { (_channel, true) }));
        }
    }
}
