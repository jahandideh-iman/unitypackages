using System;
using UnityEngine;

namespace Arman.DevelopmentConsole
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class ShortCutAttribute : Attribute
    {
        public readonly KeyCode[] keyCodes;

        public ShortCutAttribute(params KeyCode[] keyCodes)
        {
            this.keyCodes = keyCodes;
        }
    }

   
}