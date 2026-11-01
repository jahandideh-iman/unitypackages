using System;
using UnityEngine;

namespace Arman.ConfigurationManagement
{
    public class AutoFillAssetArrayAttribute : PropertyAttribute
    {
        public readonly string PropertyName;

        public AutoFillAssetArrayAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }
    }
}
