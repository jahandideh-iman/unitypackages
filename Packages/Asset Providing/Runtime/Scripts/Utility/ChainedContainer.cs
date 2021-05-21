
using System.Collections.Generic;

namespace Arman.AssetProviding.Utility
{
    public class ChainedContainer<T>
    {
        List<T> chainedObjects = new List<T>();

        public void Add(T obj)
        {
            chainedObjects.Add(obj);
        }

        public void Remove(T obj)
        {
            chainedObjects.Remove(obj);
        }

        protected List<T> ChainedObjects()
        {
            return chainedObjects;
        }
    }
}