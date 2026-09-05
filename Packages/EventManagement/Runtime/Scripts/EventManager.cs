using System.Collections.Generic;


namespace Arman.EventManagement
{
    public class EventManager : IEventManager
    {
        private List<IEventListener> listeners = new List<IEventListener>();
        List<IEventListener> listenersCopy = new List<IEventListener>();

        public void Register(IEventListener listener)
        {
            if (listeners.Contains(listener) == false)
                listeners.Add(listener);
        }

        public void UnRegister(IEventListener listener)
        {
            listeners.Remove(listener);
        }

        public void Propagate(IGameEvent evt, object sender)
        {
            listenersCopy.Clear();
            listenersCopy.AddRange(listeners);

            foreach (var listener in listenersCopy)
                listener.OnEvent(evt, sender);
        }

        public bool Has(IEventListener listener)
        {
            return listeners.Contains(listener);
        }

        public void Clear()
        {
            listeners.Clear();
        }
    }
}