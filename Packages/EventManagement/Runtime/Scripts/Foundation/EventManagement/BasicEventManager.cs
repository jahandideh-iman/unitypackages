using System.Collections.Generic;


namespace Arman.Foundation.EventManagement
{
    public class BasicEventManager : EventManager
    {
        private List<EventListener> listeners = new List<EventListener>();
        List<EventListener> listenersCopy = new List<EventListener>();

        public void Register(EventListener listener)
        {
            if (listeners.Contains(listener) == false)
                listeners.Add(listener);
        }

        public void UnRegister(EventListener listener)
        {
            listeners.Remove(listener);
        }

        public void Propagate(GameEvent evt, object sender)
        {
            listenersCopy.Clear();
            listenersCopy.AddRange(listeners);

            foreach (var listener in listenersCopy)
                listener.OnEvent(evt, sender);
        }

        public bool Has(EventListener listener)
        {
            return listeners.Contains(listener);
        }

        public void Clear()
        {
            listeners.Clear();
        }
    }
}