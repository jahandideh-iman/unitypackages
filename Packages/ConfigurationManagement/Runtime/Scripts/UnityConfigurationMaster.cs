using UnityEngine;

namespace Arman.ConfigurationManagement
{
    [CreateAssetMenu(fileName = "ConfigurationMaster", menuName = "Arman/Configuration/UnityConfigurationMaster")]
    public class UnityConfigurationMaster : ScriptableConfiguration
    {
        [AutoFillAssetArray("scriptableConfigurers")]
        public string temp;

        public ScriptableConfiguration[] scriptableConfigurers;

        public override void RegisterSelf(IConfigurationManager manager)
        {
            foreach (var config in scriptableConfigurers)
                config.RegisterSelf(manager);
        }

    }
}