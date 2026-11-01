using System.Collections.Generic;

namespace Arman.AssetProviding
{
    public class ChainedContainer<T>
    {
        List<T> _chainedObjects = new List<T>();

        public void Add(T obj)
        {
            _chainedObjects.Add(obj);
        }

        public void Remove(T obj)
        {
            _chainedObjects.Remove(obj);
        }

        protected List<T> ChainedObjects()
        {
            return _chainedObjects;
        }
    }
}
