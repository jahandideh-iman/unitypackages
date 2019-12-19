using System;

using UnityEngine;

public class AutoFillAssetArrayAttribute : PropertyAttribute
{
    public readonly string propertyName;


    public AutoFillAssetArrayAttribute(string propertyName)
    {
        this.propertyName = propertyName;
    }
}
