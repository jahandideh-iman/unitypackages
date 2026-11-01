using System.Collections.Generic;

namespace Arman.EventManagement
{
    public class EventManager : IEventManager
    {
        private List<IEventListener> _listeners = new List<IEventListener>();
        List<IEventListener> _listenersCopy = new List<IEventListener>();

        public void Register(IEventListener listener)
        {
            if (_listeners.Contains(listener) == false)
                _listeners.Add(listener);
        }

        public void UnRegister(IEventListener listener)
        {
            _listeners.Remove(listener);
        }

        public void Propagate(IGameEvent evt, object sender)
        {
            _listenersCopy.Clear();
            _listenersCopy.AddRange(_listeners);

            foreach (var listener in _listenersCopy)
                listener.OnEvent(evt, sender);
        }

        public bool Has(IEventListener listener)
        {
            return _listeners.Contains(listener);
        }

        public void Clear()
        {
            _listeners.Clear();
        }
    }
}
