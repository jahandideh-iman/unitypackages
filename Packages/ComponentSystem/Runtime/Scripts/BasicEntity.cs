
using System.Collections.Generic;

namespace Arman.ComponentSystem
{
    public class BasicEntity : IEntity
    {
        IComponent[] compArray = new IComponent[0];
        List<IComponent> compList = new List<IComponent>(32);

        public void AddComponent(IComponent component)
        {
            compList.Add(component);
            compArray = compList.ToArray();
            OnComponentAdded(component);
        }

        public void AddComponents(params IComponent[] components)
        {
            var length = components.Length;
            for (int i = 0; i < length; ++i)
            {
                var component = components[i];
                compList.Add(component);
                OnComponentAdded(component);
            }
            compArray = compList.ToArray();
        }

        protected virtual void OnComponentAdded(IComponent component) { }

        public IEnumerable<IComponent> AllComponents()
        {
            return compArray;
        }

        public T GetComponent<T>() where T : IComponent
        {
            var count = compArray.Length;
            for (int i = 0; i < count; ++i)
                if (compArray[i] is T)
                    return (T)compArray[i];

            return default(T);
        }

        public T GetComponentFromEnd<T>() where T : IComponent
        {
            var count = compArray.Length;
            for (int i = count-1; i >= 0; --i)
                if (compArray[i] is T)
                    return (T)compArray[i];

            return default(T);
        }

        public T GetComponent<T>(int index) where T : IComponent
        {
            return (T)compArray[index];
        }
    }
}