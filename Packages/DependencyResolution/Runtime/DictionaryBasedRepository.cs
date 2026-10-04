using System;
using System.Collections.Generic;

namespace Arman.DependencyResolution
{
    internal class DictionaryBasedRepository : IRepository
    {
        private Dictionary<Type, object> _internalRepo;

        public DictionaryBasedRepository(Dictionary<Type, object> internalRepo)
        {
            _internalRepo = internalRepo;
        }

        public T Get<T>()
        {
            if (TryGet<T>(out var instance))
            {
                return instance;
            }

            throw new Exception($"No instance of type {typeof(T)} was found");
        }

        public bool TryGet<T>(out T instance)
        {
            if (_internalRepo.TryGetValue(typeof(T), out var obj))
            {
                instance = (T)obj;
                return true;
            }

            instance = default!;
            return false;
        }
    }
}
