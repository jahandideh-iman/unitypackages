using Arman.Foundation.Core.ConfigurationManagement;
using UnityEngine;

namespace Arman.Foundation.Unity.Configuration
{
    [CreateAssetMenu(fileName = "ConfigurationMaster", menuName = "Arman/Configuration/UnityConfigurationMaster")]
    public class UnityConfigurationMaster : ScriptableConfiguration
    {
        [AutoFillAssetArray("scriptableConfigurers")]
        public string temp;

        public ScriptableConfiguration[] scriptableConfigurers;

        public override void RegisterSelf(ConfigurationManager manager)
        {
            foreach (var config in scriptableConfigurers)
                config.RegisterSelf(manager);
        }

    }
}