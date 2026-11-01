using System;
using System.Collections.Generic;
using Arman.EventManagement;
using Moq;
using NUnit.Framework;

namespace Arman.EventManagement.Tests
{
    public class EventManagerTest
    {
        // IGameEvent is an empty marker, so a real instance says more than a proxy:
        // the tests care about which object came back, not about calls made on it.
        class FakeGameEvent : IGameEvent { }

        // Keeps every event it receives, so a test can read back what reached it and in
        // which order. OnEventAction lets a test act from inside a propagation.
        class FakeEventListener : IEventListener
        {
            public readonly List<(IGameEvent evt, object sender)> Received =
                new List<(IGameEvent evt, object sender)>();

            public Action? OnEventAction;

            public void OnEvent(IGameEvent evt, object sender)
            {
                Received.Add((evt, sender));
                OnEventAction?.Invoke();
            }
        }

        IEventManager _manager;

        Mock<IEventListener> _listener1;
        Mock<IEventListener> _listener2;

        [SetUp]
        public void Setup()
        {
            _manager = new EventManager();

            _listener1 = new Mock<IEventListener>();
            _listener2 = new Mock<IEventListener>();
        }

        [Test]
        public void RegisteringListenerShouldAddThemToManager()
        {
            _manager.Register(_listener1.Object);
            _manager.Register(_listener2.Object);

            Assert.That(_manager.Has(_listener1.Object));
            Assert.That(_manager.Has(_listener2.Object));
        }

        [Test]
        public void UnregisteringListenerShouldRemoveThemFromManager()
        {
            _manager.Register(_listener1.Object);

            _manager.UnRegister(_listener1.Object);

            Assert.That(_manager.Has(_listener1.Object), Is.False);
        }

        [Test]
        public void PropagatingAnEventShouldNotifyRegisteredListeners()
        {
            _manager.Register(_listener1.Object);
            _manager.Register(_listener2.Object);

            IGameEvent evt = new FakeGameEvent();
            _manager.Propagate(evt, this);

            _listener1.Verify(listener => listener.OnEvent(evt, this), Times.Once);
            _listener2.Verify(listener => listener.OnEvent(evt, this), Times.Once);
        }

        [Test]
        public void PropagatingAnEventShouldNotNotifyUnRegisteredListeners()
        {
            _manager.Register(_listener1.Object);
            _manager.UnRegister(_listener1.Object);

            IGameEvent evt = new FakeGameEvent();
            _manager.Propagate(evt, this);
            _manager.Propagate(evt, this);

            _listener1.Verify(
                listener => listener.OnEvent(It.IsAny<IGameEvent>(), It.IsAny<object>()),
                Times.Never
            );
        }

        [Test]
        public void ClearingShouldRemoveAllListeners()
        {
            _manager.Register(_listener1.Object);
            _manager.Register(_listener2.Object);

            _manager.Clear();

            Assert.That(_manager.Has(_listener1.Object), Is.False);
            Assert.That(_manager.Has(_listener2.Object), Is.False);
        }

        [Test]
        public void PropagatingAnEventShouldGiveTheEventAndTheSenderToTheListener()
        {
            var listener = new FakeEventListener();
            _manager.Register(listener);

            var evt = new FakeGameEvent();
            var sender = new object();
            _manager.Propagate(evt, sender);

            Assert.That(listener.Received, Is.EqualTo(new[] { ((IGameEvent)evt, sender) }));
        }

        [Test]
        public void RegisteringAListenerTwiceShouldNotifyItOnlyOnce()
        {
            var listener = new FakeEventListener();

            _manager.Register(listener);
            _manager.Register(listener);

            _manager.Propagate(new FakeGameEvent(), this);

            Assert.That(listener.Received, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnregisteringAListenerThatWasRegisteredTwiceShouldRemoveIt()
        {
            var listener = new FakeEventListener();

            _manager.Register(listener);
            _manager.Register(listener);
            _manager.UnRegister(listener);

            Assert.That(_manager.Has(listener), Is.False);
        }

        [Test]
        public void UnregisteringAnUnknownListenerShouldNotThrow()
        {
            Assert.That(() => _manager.UnRegister(new FakeEventListener()), Throws.Nothing);
        }

        [Test]
        public void PropagatingWithoutListenersShouldNotThrow()
        {
            Assert.That(() => _manager.Propagate(new FakeGameEvent(), this), Throws.Nothing);
        }

        [Test]
        public void EveryPropagatedEventShouldReachTheListenerInOrder()
        {
            var listener = new FakeEventListener();
            _manager.Register(listener);

            var first = new FakeGameEvent();
            var second = new FakeGameEvent();
            _manager.Propagate(first, this);
            _manager.Propagate(second, this);

            Assert.That(listener.Received[0].evt, Is.SameAs(first));
            Assert.That(listener.Received[1].evt, Is.SameAs(second));
        }

        [Test]
        public void AListenerUnregisteringItselfDuringPropagationShouldNotStopTheOthers()
        {
            var selfRemoving = new FakeEventListener();
            var other = new FakeEventListener();
            selfRemoving.OnEventAction = () => _manager.UnRegister(selfRemoving);

            _manager.Register(selfRemoving);
            _manager.Register(other);

            _manager.Propagate(new FakeGameEvent(), this);
            _manager.Propagate(new FakeGameEvent(), this);

            Assert.That(selfRemoving.Received, Has.Count.EqualTo(1));
            Assert.That(other.Received, Has.Count.EqualTo(2));
            Assert.That(_manager.Has(selfRemoving), Is.False);
        }

        [Test]
        public void AListenerRegisteredDuringPropagationShouldOnlyReceiveLaterEvents()
        {
            var late = new FakeEventListener();
            var registering = new FakeEventListener();
            registering.OnEventAction = () => _manager.Register(late);

            _manager.Register(registering);

            _manager.Propagate(new FakeGameEvent(), this);
            Assert.That(late.Received, Is.Empty);

            _manager.Propagate(new FakeGameEvent(), this);
            Assert.That(late.Received, Has.Count.EqualTo(1));
        }
    }
}
