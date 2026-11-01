using System;
using System.Reflection;
using UnityEngine;

namespace Arman.DevelopmentConsole
{
    public class CommandInfo
    {
        public readonly string Name;
        public MethodInfo MethodInfo;
        public KeyCode[] KeyCodes = new KeyCode[0];

        public CommandInfo(string name, MethodInfo methodInfo)
        {
            this.Name = name;
            this.MethodInfo = methodInfo;
        }

        public void Invoke()
        {
            MethodInfo.Invoke(null, null);
        }

        public void Invoke(object[] inputs)
        {
            MethodInfo.Invoke(null, inputs);
        }

        public bool HasNoInput()
        {
            return MethodInfo.GetParameters().Length == 0;
        }

        public void SetShortcut(KeyCode[] keyCodes)
        {
            this.KeyCodes = keyCodes;
        }
    }
}
