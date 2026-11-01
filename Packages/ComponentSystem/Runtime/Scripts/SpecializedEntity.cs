using System.Collections.Generic;
using System.Linq;

namespace Arman.ComponentSystem
{
    public class SpecializedEntity<T> : ISpecializedEntity<T>
        where T : IComponent
    {
        Entity _basicEntity = new Entity();

        List<T> _compList = new List<T>();

        public void AddComponent(T component)
        {
            _basicEntity.AddComponent(component);
            _compList.Add(component);
        }

        public List<T> AllComponents()
        {
            return _compList;
        }

        public U GetComponent<U>()
            where U : T
        {
            return _basicEntity.GetComponent<U>();
        }
    }
}
