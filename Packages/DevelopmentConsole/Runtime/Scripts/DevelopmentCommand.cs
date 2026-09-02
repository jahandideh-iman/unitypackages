using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{

    public class DevelopmentCommand : MonoBehaviour
    {
        public Text commandNameText;
        public Text shortcutText;

        CommandInfo commandInfo;

        internal void Init(CommandInfo commandInfo)
        {
            this.commandInfo = commandInfo;
            commandNameText.text = commandInfo.name;
            shortcutText.text = string.Join("+", commandInfo.keyCodes.Select(k => k.ToString()));
        }

        public void Execute()
        {
            if (commandInfo.HasNoInput())
                commandInfo.Invoke();
            else
                GameObject.FindObjectOfType<DevelopmentConsolePanel>().OpenCommandInputPromtFor(commandInfo);
        }
    }
}