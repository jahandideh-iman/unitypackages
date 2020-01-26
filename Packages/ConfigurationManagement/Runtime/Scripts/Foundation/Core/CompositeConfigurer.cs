using System.Collections.Generic;

namespace Arman.Foundation.Core.ConfigurationManagement
{
    public class CompositeConfigurer<T> : Configurer<T>
    {
        private List<Configurer<T>> configurers = new List<Configurer<T>>();

        public void AddConfigurer(Configurer<T> configurer)
        {
            this.configurers.Add(configurer);
        }
        
        public void Configure(T entity)
        {
            foreach (var configurer in configurers)
                configurer.Configure(entity);
        }

        public void RegisterSelf(ConfigurationManager manager)
        {
            manager.Register(this);
        }
    }
}
