using System;
using System.Collections.Generic;


namespace Arman.Foundation.Core.ServiceLocating
{
    public class ServiceLocator
    {
        private static ServiceLocator instance;

        private List<object> services = new List<object>();

        public static void Init()
        {
            if (instance == null)
                instance = new ServiceLocator();
        }

        public static bool IsInited()
        {
            return instance != null;
        }

        public static void Clear()
        {
            instance = null;
        }

        public static void Register<TInterface, TImplementation>(TImplementation implementation) where TImplementation : TInterface
        {
            instance.services.Add(implementation);
        }

        public static void UnRegister<T>()
        {
            var service = Find<T>();
            instance.services.Remove(service);
        }

        public static void Replace<TInterface, TImplementation>(TImplementation implementation) where TImplementation : TInterface
        {
            UnRegister<TInterface>();
            Register<TInterface, TImplementation>(implementation);
        }

        public static T Find<T>()
        {
            foreach (var service in instance.services)
                if (service is T)
                    return (T)service;

            throw new System.Exception(string.Format("Service of type '{0}' could not be found.", typeof(T).ToString()));
        }
    }
}