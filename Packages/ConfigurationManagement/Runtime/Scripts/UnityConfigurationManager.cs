
using UnityEngine;

namespace Arman.ConfigurationManagement
{
    public class UnityConfigurationManager : MonoBehaviour, IConfigurationManager
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

        public bool Contains<T>(IConfigurer<T> configurer)
        {
            return internalConfigManager.Contains<T>(configurer);
        }

        public IConfigurer<T> FindConfigurer<T>()
        {
            return internalConfigManager.FindConfigurer<T>();
        }

        public void Register<T>(IConfigurer<T> configurer)
        {
            internalConfigManager.Register<T>(configurer);
        }

        public IConfigurer<T> RemoveConfigurer<T>()
        {
            return internalConfigManager.RemoveConfigurer<T>();
        }
    }
}