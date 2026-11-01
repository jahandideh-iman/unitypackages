using System;
using System.Collections.Generic;

namespace Arman.ConfigurationManagement
{
    public class ConfigurationManager : IConfigurationManager
    {
        private Dictionary<Type, IConfigurer> _configurers = new Dictionary<Type, IConfigurer>();

        public void Register<T>(IConfigurer<T> configurer)
        {
            _configurers[typeof(T)] = configurer;
        }

        public void Configure<T>(T target)
        {
            FindConfigurer<T>().Configure(target);
        }

        public bool Contains<T>(IConfigurer<T> configurer)
        {
            return _configurers.ContainsKey(typeof(T));
        }

        public IConfigurer<T> FindConfigurer<T>()
        {
            if (_configurers.ContainsKey(typeof(T)) == false)
                return null;

            return _configurers[typeof(T)] as IConfigurer<T>;
        }

        public IConfigurer<T> RemoveConfigurer<T>()
        {
            var configurer = FindConfigurer<T>();

            if (configurer != null)
                _configurers.Remove(typeof(T));

            return configurer;
        }
    }
}
