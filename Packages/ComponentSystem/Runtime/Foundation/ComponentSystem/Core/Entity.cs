
using System.Collections.Generic;

namespace Arman.Foundation.ComponentSystem.Core
{
    public interface Entity 
    {
        void AddComponent(Component component);
        T GetComponent<T>() where T : Component;

        IEnumerable<Component> AllComponents();
    }
}