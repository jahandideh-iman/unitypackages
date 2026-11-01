using System.Collections.Generic;

namespace Arman.ConfigurationManagement
{
    public class CompositeConfigurer<T> : IConfigurer<T>
    {
        private List<IConfigurer<T>> _configurers = new List<IConfigurer<T>>();

        public void AddConfigurer(IConfigurer<T> configurer)
        {
            this._configurers.Add(configurer);
        }

        public void Configure(T entity)
        {
            foreach (var configurer in _configurers)
                configurer.Configure(entity);
        }

        public void RegisterSelf(IConfigurationManager manager)
        {
            manager.Register(this);
        }
    }
}
