using System.Collections.Generic;

namespace Arman.Utility.Core
{
    public interface Container<T>
    {
        U Find<U>() where U : T;

        ICollection<U> FindAll<U>() where U : T;

        void Add(T item);

        bool Contains(T item);

        IEnumerable<T> Items();

    }

}