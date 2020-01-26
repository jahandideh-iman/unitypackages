using System;
using System.Reflection;
using UnityEngine;

namespace Arman.Development.DevelopmentConsole.Base
{
    public class CommandInfo
    {
        public readonly string name;
        public MethodInfo methodInfo;
        public KeyCode[] keyCodes = new KeyCode[0];

        public CommandInfo(string name, MethodInfo methodInfo)
        {
            this.name = name;
            this.methodInfo = methodInfo;
        }

        public void Invoke()
        {
            methodInfo.Invoke(null, null);
        }

        public void Invoke(object[] inputs)
        {
            methodInfo.Invoke(null, inputs);
        }

        public bool HasNoInput()
        {
            return methodInfo.GetParameters().Length == 0;
        }

        public void SetShortcut(KeyCode[] keyCodes)
        {
            this.keyCodes = keyCodes;
        }
    }
}