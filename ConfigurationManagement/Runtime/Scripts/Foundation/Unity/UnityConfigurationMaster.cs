
using System;
using System.Collections;
using System.Collections.Generic;
using Match3.Foundation.Base.Configuration;
using UnityEngine;

namespace Match3.Foundation.Unity.Configuration
{
    [CreateAssetMenu(fileName = "ConfigurationMaster", menuName = "PandasCanPlay/Configuration/UnityConfigurationMaster")]
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