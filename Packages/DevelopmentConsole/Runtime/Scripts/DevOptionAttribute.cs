using System;
using UnityEngine.Scripting;

namespace Arman.DevelopmentConsole
{
    [AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
    public class DevOptionAttribute : PreserveAttribute
    {
        public readonly string Group;
        public readonly string CommandName;

        public DevOptionAttribute(string group, string commandName)
        {
            this.Group = group;
            this.CommandName = commandName;
        }
    }
}
