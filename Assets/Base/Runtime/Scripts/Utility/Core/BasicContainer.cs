using System.Collections.Generic;

namespace Arman.Utility.Core
{
    public class BasicContainer<T> : Container<T>
    {
        List<T> items = new List<T>();
        public U Find<U>() where U : T
        {
            return (U)items.Find(i => i is U);
        }

        public ICollection<U> FindAll<U>() where U : T
        {
            return items.FindAll(i => i is U).ConvertAll(i=> (U)i);
        }

        public void Add(T item)
        {
            items.Add(item);











        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        public IEnumerable<T> Items()
        {
            return items;
        }

    }

}