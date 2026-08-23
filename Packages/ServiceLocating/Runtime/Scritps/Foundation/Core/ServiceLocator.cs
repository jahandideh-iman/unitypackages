using System;
using System.Collections.Generic;


namespace Arman.Foundation.Core.ServiceLocating
{
    public class ServiceLocator
    {
        private static ServiceLocator instance;

        private List<IService> services = new List<IService>();

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

        public static void Register(IService service)
        {
            instance.services.Add(service);
        }

        public static void UnRegister<T>() where T : IService
        {
            var service = Find<T>();
            instance.services.Remove(service);
        }

        public static void Replace<T>(T service) where T : IService
        {
            UnRegister<T>();
            Register(service);
        }

        public static T Find<T>() where T : IService
        {
            foreach (var service in instance.services)
                if (service is T)
                    return (T)service;

            throw new System.Exception(string.Format("Service of type '{0}' could not be found.", typeof(T).ToString()));
        }
    }
}