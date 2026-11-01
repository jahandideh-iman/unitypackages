using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.ConfigurationManagement
{
    public class UnityConfigurationManager : MonoBehaviour, IConfigurationManager
    {
        [FormerlySerializedAs("configurationMaster")]
        public UnityConfigurationMaster ConfigurationMaster;

        ConfigurationManager _internalConfigManager = new ConfigurationManager();

        public void Init()
        {
            ConfigurationMaster.RegisterSelf(this);
            //foreach (var configurer in configurationMaster.Configurations())
            //    configurer.RegisterSelf(this);
        }

        public void Configure<T>(T target)
        {
            _internalConfigManager.Configure<T>(target);
        }

        public bool Contains<T>(IConfigurer<T> configurer)
        {
            return _internalConfigManager.Contains<T>(configurer);
        }

        public IConfigurer<T> FindConfigurer<T>()
        {
            return _internalConfigManager.FindConfigurer<T>();
        }

        public void Register<T>(IConfigurer<T> configurer)
        {
            _internalConfigManager.Register<T>(configurer);
        }

        public IConfigurer<T> RemoveConfigurer<T>()
        {
            return _internalConfigManager.RemoveConfigurer<T>();
        }
    }
}
