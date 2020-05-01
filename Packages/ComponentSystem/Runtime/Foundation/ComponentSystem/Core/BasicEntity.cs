
using System.Collections.Generic;

namespace Arman.Foundation.ComponentSystem.Core
{
    public class BasicEntity : Entity
    {
        Component[] compArray = new Component[0];
        List<Component> compList = new List<Component>(32);

        public void AddComponent(Component component)
        {
            compList.Add(component);
            compArray = compList.ToArray();
            OnComponentAdded(component);
        }

        public void AddComponents(params Component[] components)
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

        protected virtual void OnComponentAdded(Component component) { }

        public IEnumerable<Component> AllComponents()
        {
            return compArray;
        }

        public T GetComponent<T>() where T : Component
        {
            var count = compArray.Length;
            for (int i = 0; i < count; ++i)
                if (compArray[i] is T)
                    return (T)compArray[i];

            return default(T);
        }

        public T GetComponentFromEnd<T>() where T : Component
        {
            var count = compArray.Length;
            for (int i = count-1; i >= 0; --i)
                if (compArray[i] is T)
                    return (T)compArray[i];

            return default(T);
        }

        public T GetComponent<T>(int index) where T : Component
        {
            return (T)compArray[index];
        }
    }
}