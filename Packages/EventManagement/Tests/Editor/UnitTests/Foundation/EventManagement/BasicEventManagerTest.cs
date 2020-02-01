using NUnit.Framework;
using Arman.Foundation.EventManagement;

namespace Arman.Tests.Foundation.EventManagement
{
    public class EventManagerTest
    {

        class ListenerMock : EventListener
        {
            public GameEvent evt = null;

            public void OnEvent(GameEvent evt, object sender)
            {
                this.evt = evt;
            }

        }

        class EventMock : GameEvent
        {

        }


        EventManager manager;

        ListenerMock listener1;
        ListenerMock listener2;

        [SetUp]
        public void Setup()
        {
            manager = new BasicEventManager();

            listener1 = new ListenerMock();
            listener2 = new ListenerMock();
        }

        [Test]
        public void RegisteringListenrerShouldAddThemToManager()
        {
            manager.Register(listener1);
            manager.Register(listener2);

            Assert.That(manager.Has(listener1));
            Assert.That(manager.Has(listener2));
        }


        [Test]
        public void UnregisteringListenrerShouldRemoveThemFromManager()
        {
            manager.Register(listener1);

            manager.UnRegister(listener1);

            Assert.That(manager.Has(listener1), Is.False);
        }

        [Test]
        public void PropagatingAnEventShouldNotifyRegisteredListeners()
        {
            manager.Register(listener1);
            manager.Register(listener2);

            GameEvent evt = new EventMock();
            manager.Propagate(evt, this);

            Assert.That(listener1.evt, Is.SameAs(evt));
            Assert.That(listener2.evt, Is.SameAs(evt));
        }

        [Test]
        public void PropagatingAnEventShouldNotNotifyUnRegisteredListeners()
        {
            manager.Register(listener1);
            manager.UnRegister(listener1);

            GameEvent evt = new EventMock();
            manager.Propagate(evt, this);
            manager.Propagate(evt, this);

            Assert.That(listener1.evt, Is.Null);
        }

        [Test]
        public void ClearingShouldRemoveAllListeners()
        {
            manager.Register(listener1);
            manager.Register(listener2);

            manager.Clear();

            Assert.That(manager.Has(listener1), Is.False);
            Assert.That(manager.Has(listener2), Is.False);
        }
    }

}