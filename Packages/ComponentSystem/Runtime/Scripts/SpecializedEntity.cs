using System.Collections.Generic;
using System.Linq;

namespace Arman.ComponentSystem
{
    public class SpecializedEntity<T> : ISpecializedEntity<T> where T :IComponent
    {
        Entity basicEntity = new Entity();

        List<T> compList = new List<T>();

        public void AddComponent(T component)
        {
            basicEntity.AddComponent(component);
            compList.Add(component);
        }

        public List<T> AllComponents()
        {
            return compList;
        }

        public U GetComponent<U>() where U : T
        {
            return basicEntity.GetComponent<U>();
        }
    }

}