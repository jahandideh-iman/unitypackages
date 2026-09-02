using System.Collections;
using System.Collections.Generic;

namespace Arman.ComponentSystem
{
    public interface ISpecializedEntity<T> where T : IComponent
    {
        void AddComponent(T component);
        U GetComponent<U>() where U : T;

        List<T> AllComponents();
    }
}