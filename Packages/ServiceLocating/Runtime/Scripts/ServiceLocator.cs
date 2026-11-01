using System;
using System.Collections.Generic;

namespace Arman.ServiceLocating
{
    public class ServiceLocator
    {
        private static ServiceLocator _instance;

        private List<object> _services = new List<object>();

        public static void Init()
        {
            if (_instance == null)
                _instance = new ServiceLocator();
        }

        public static bool IsInited()
        {
            return _instance != null;
        }

        public static void Clear()
        {
            _instance = null;
        }

        public static void Register<TInterface, TImplementation>(TImplementation implementation)
            where TImplementation : TInterface
        {
            _instance._services.Add(implementation);
        }

        public static void UnRegister<T>()
        {
            var service = Find<T>();
            _instance._services.Remove(service);
        }

        public static void Replace<TInterface, TImplementation>(TImplementation implementation)
            where TImplementation : TInterface
        {
            UnRegister<TInterface>();
            Register<TInterface, TImplementation>(implementation);
        }

        public static T Find<T>()
        {
            foreach (var service in _instance._services)
                if (service is T)
                    return (T)service;

            throw new System.Exception(
                string.Format("Service of type '{0}' could not be found.", typeof(T).ToString())
            );
        }
    }
}
