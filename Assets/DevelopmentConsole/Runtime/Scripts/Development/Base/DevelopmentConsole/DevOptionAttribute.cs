using System;
using UnityEngine.Scripting;


namespace Arman.Development.DevelopmentConsole.Base
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class DevOptionAttribute : PreserveAttribute
    {
        public readonly string group;
        public readonly string commandName;

        public DevOptionAttribute(string group, string commandName)
        {
            this.group = group;
            this.commandName = commandName;
        }
    }
}