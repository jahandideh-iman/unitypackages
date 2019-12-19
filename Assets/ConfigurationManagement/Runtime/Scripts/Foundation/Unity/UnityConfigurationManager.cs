
using Arman.Foundation.Core.ConfigurationManagement;
using UnityEngine;

namespace Arman.Foundation.Unity.Configuration
{
    public class UnityConfigurationManager : MonoBehaviour, ConfigurationManager
    {
        public UnityConfigurationMaster configurationMaster;

        BasicConfigurationManager internalConfigManager = new BasicConfigurationManager();

        public void Init()
        {
            configurationMaster.RegisterSelf(this);
            //foreach (var configurer in configurationMaster.Configurations())
            //    configurer.RegisterSelf(this);
        }

        public void Configure<T>(T target)
        {
            internalConfigManager.Configure<T>(target);
        }

        public bool Contains<T>(Configurer<T> configurer)
        {
            return internalConfigManager.Contains<T>(configurer);
        }

        public Configurer<T> FindConfigurer<T>()
        {
            return internalConfigManager.FindConfigurer<T>();
        }

        public void Register<T>(Configurer<T> configurer)
        {
            internalConfigManager.Register<T>(configurer);
        }

        public Configurer<T> RemoveConfigurer<T>()
        {
            return internalConfigManager.RemoveConfigurer<T>();
        }
    }
}