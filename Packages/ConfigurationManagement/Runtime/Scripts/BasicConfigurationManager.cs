using System;
using System.Collections.Generic;

namespace Arman.ConfigurationManagement
{
    public class BasicConfigurationManager : IConfigurationManager
    {
        private Dictionary<Type, IConfigurer> configurers = new Dictionary<Type, IConfigurer>();

        public void Register<T>(IConfigurer<T> configurer)
        {
            configurers[typeof(T)] = configurer;
        }

        public void Configure<T>(T target)
        {
            FindConfigurer<T>().Configure(target);
        }
        
        public bool Contains<T>(IConfigurer<T> configurer)
        {
            return configurers.ContainsKey(typeof(T));
        }

        public IConfigurer<T> FindConfigurer<T>()
        {
            if (configurers.ContainsKey(typeof(T)) == false)
                return null;

            return configurers[typeof(T)] as IConfigurer<T>;
        }


        public IConfigurer<T> RemoveConfigurer<T>()
        {
            var configurer = FindConfigurer<T>();

            if (configurer != null)
                configurers.Remove(typeof(T));

            return configurer;
        }
    }
}