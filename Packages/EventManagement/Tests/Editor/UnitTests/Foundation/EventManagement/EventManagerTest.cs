using Arman.EventManagement;
using Moq;
using NUnit.Framework;

namespace Arman.EventManagement.Tests
{
    public class EventManagerTest
    {
        // IGameEvent is an empty marker, so a real instance says more than a proxy:
        // the tests care about which object came back, not about calls made on it.
        class FakeGameEvent : IGameEvent
        {

        }

        IEventManager manager;

        Mock<IEventListener> listener1;
        Mock<IEventListener> listener2;

        [SetUp]
        public void Setup()
        {
            manager = new EventManager();

            listener1 = new Mock<IEventListener>();
            listener2 = new Mock<IEventListener>();
        }

        [Test]
        public void RegisteringListenrerShouldAddThemToManager()
        {
            manager.Register(listener1.Object);
            manager.Register(listener2.Object);

            Assert.That(manager.Has(listener1.Object));
            Assert.That(manager.Has(listener2.Object));
        }


        [Test]
        public void UnregisteringListenrerShouldRemoveThemFromManager()
        {
            manager.Register(listener1.Object);

            manager.UnRegister(listener1.Object);

            Assert.That(manager.Has(listener1.Object), Is.False);
        }

        [Test]
        public void PropagatingAnEventShouldNotifyRegisteredListeners()
        {
            manager.Register(listener1.Object);
            manager.Register(listener2.Object);

            IGameEvent evt = new FakeGameEvent();
            manager.Propagate(evt, this);

            listener1.Verify(listener => listener.OnEvent(evt, this), Times.Once);
            listener2.Verify(listener => listener.OnEvent(evt, this), Times.Once);
        }

        [Test]
        public void PropagatingAnEventShouldNotNotifyUnRegisteredListeners()
        {
            manager.Register(listener1.Object);
            manager.UnRegister(listener1.Object);

            IGameEvent evt = new FakeGameEvent();
            manager.Propagate(evt, this);
            manager.Propagate(evt, this);

            listener1.Verify(
                listener => listener.OnEvent(It.IsAny<IGameEvent>(), It.IsAny<object>()),
                Times.Never);
        }

        [Test]
        public void ClearingShouldRemoveAllListeners()
        {
            manager.Register(listener1.Object);
            manager.Register(listener2.Object);

            manager.Clear();

            Assert.That(manager.Has(listener1.Object), Is.False);
            Assert.That(manager.Has(listener2.Object), Is.False);
        }
    }

}
