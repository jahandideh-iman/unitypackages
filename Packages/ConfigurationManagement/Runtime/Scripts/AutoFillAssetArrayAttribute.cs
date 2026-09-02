using System;

using UnityEngine;

namespace Arman.ConfigurationManagement
{
    public class AutoFillAssetArrayAttribute : PropertyAttribute
    {
        public readonly string propertyName;


        public AutoFillAssetArrayAttribute(string propertyName)
        {
            this.propertyName = propertyName;
        }
    }
}
