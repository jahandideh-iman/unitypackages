using System;
using UnityEngine;

namespace Arman.DevelopmentConsole
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class ShortCutAttribute : Attribute
    {
        public readonly KeyCode[] KeyCodes;

        public ShortCutAttribute(params KeyCode[] keyCodes)
        {
            this.KeyCodes = keyCodes;
        }
    }
}
