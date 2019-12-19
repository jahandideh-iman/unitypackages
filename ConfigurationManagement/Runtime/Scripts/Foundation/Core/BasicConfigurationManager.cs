using System;
using System.Collections.Generic;

namespace Arman.Foundation.Core.ConfigurationManagement
{
    public class BasicConfigurationManager : ConfigurationManager
    {
        private Dictionary<Type, Configurer> configurers = new Dictionary<Type, Configurer>();

        public void Register<T>(Configurer<T> configurer)
        {
            configurers[typeof(T)] = configurer;
        }

        public void Configure<T>(T target)
        {
            FindConfigurer<T>().Configure(target);
        }
        
        public bool Contains<T>(Configurer<T> configurer)
        {
            return configurers.ContainsKey(typeof(T));
        }

        public Configurer<T> FindConfigurer<T>()
        {
            if (configurers.ContainsKey(typeof(T)) == false)
                return null;

            return configurers[typeof(T)] as Configurer<T>;
        }


        public Configurer<T> RemoveConfigurer<T>()
        {
            var configurer = FindConfigurer<T>();

            if (configurer != null)
                configurers.Remove(typeof(T));

            return configurer;
        }
    }
}