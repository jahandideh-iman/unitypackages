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
    }
}
