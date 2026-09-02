
using System.Collections.Generic;

namespace Arman.ComponentSystem
{
    public interface IEntity 
    {
        void AddComponent(IComponent component);
        T GetComponent<T>() where T : IComponent;

        IEnumerable<IComponent> AllComponents();
    }
}