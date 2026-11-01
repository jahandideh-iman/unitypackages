using System.Collections.Generic;

namespace Arman.ComponentSystem
{
    public class Entity : IEntity
    {
        IComponent[] _compArray = new IComponent[0];
        List<IComponent> _compList = new List<IComponent>(32);

        public void AddComponent(IComponent component)
        {
            _compList.Add(component);
            _compArray = _compList.ToArray();
            OnComponentAdded(component);
        }

        public void AddComponents(params IComponent[] components)
        {
            var length = components.Length;
            for (int i = 0; i < length; ++i)
            {
                var component = components[i];
                _compList.Add(component);
                OnComponentAdded(component);
            }
            _compArray = _compList.ToArray();
        }

        protected virtual void OnComponentAdded(IComponent component) { }

        public IEnumerable<IComponent> AllComponents()
        {
            return _compArray;
        }

        public T GetComponent<T>()
            where T : IComponent
        {
            var count = _compArray.Length;
            for (int i = 0; i < count; ++i)
                if (_compArray[i] is T)
                    return (T)_compArray[i];

            return default(T);
        }

        public T GetComponentFromEnd<T>()
            where T : IComponent
        {
            var count = _compArray.Length;
            for (int i = count - 1; i >= 0; --i)
                if (_compArray[i] is T)
                    return (T)_compArray[i];

            return default(T);
        }

        public T GetComponent<T>(int index)
            where T : IComponent
        {
            return (T)_compArray[index];
        }
    }
}
