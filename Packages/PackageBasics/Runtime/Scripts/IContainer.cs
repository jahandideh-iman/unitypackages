using System.Collections.Generic;

namespace Arman.PackageBasics
{
    public interface IContainer<T>
    {
        U Find<U>() where U : T;

        ICollection<U> FindAll<U>() where U : T;

        void Add(T item);

        bool Contains(T item);

        IEnumerable<T> Items();

    }

}