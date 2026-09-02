using System.Collections.Generic;

namespace Arman.ConfigurationManagement
{
    public class CompositeConfigurer<T> : IConfigurer<T>
    {
        private List<IConfigurer<T>> configurers = new List<IConfigurer<T>>();

        public void AddConfigurer(IConfigurer<T> configurer)
        {
            this.configurers.Add(configurer);
        }
        
        public void Configure(T entity)
        {
            foreach (var configurer in configurers)
                configurer.Configure(entity);
        }

        public void RegisterSelf(IConfigurationManager manager)
        {
            manager.Register(this);
        }
    }
}
