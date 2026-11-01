using System.Collections.Generic;

namespace Arman.PackageBasics
{
    public class Container<T> : IContainer<T>
    {
        List<T> _items = new List<T>();

        public U Find<U>()
            where U : T
        {
            return (U)_items.Find(i => i is U);
        }

        public ICollection<U> FindAll<U>()
            where U : T
        {
            return _items.FindAll(i => i is U).ConvertAll(i => (U)i);
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public IEnumerable<T> Items()
        {
            return _items;
        }
    }
}
