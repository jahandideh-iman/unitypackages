
using System.Collections.Generic;

namespace Arman.Foundation.ComponentSystem.Core
{
    public interface IEntity 
    {
        void AddComponent(IComponent component);
        T GetComponent<T>() where T : IComponent;

        IEnumerable<IComponent> AllComponents();
    }
}