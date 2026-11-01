using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.ConfigurationManagement
{
    [CreateAssetMenu(
        fileName = "ConfigurationMaster",
        menuName = "Arman/Configuration/UnityConfigurationMaster"
    )]
    public class UnityConfigurationMaster : ScriptableConfiguration
    {
        [FormerlySerializedAs("temp")]
        [AutoFillAssetArray("ScriptableConfigurers")]
        public string Temp;

        [FormerlySerializedAs("scriptableConfigurers")]
        public ScriptableConfiguration[] ScriptableConfigurers;

        public override void RegisterSelf(IConfigurationManager manager)
        {
            foreach (var config in ScriptableConfigurers)
                config.RegisterSelf(manager);
        }
    }
}
