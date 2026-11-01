using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    public class DevelopmentCommand : MonoBehaviour
    {
        [FormerlySerializedAs("commandNameText")]
        public Text CommandNameText;

        [FormerlySerializedAs("shortcutText")]
        public Text ShortcutText;

        CommandInfo _commandInfo;

        internal void Init(CommandInfo commandInfo)
        {
            this._commandInfo = commandInfo;
            CommandNameText.text = commandInfo.Name;
            ShortcutText.text = string.Join("+", commandInfo.KeyCodes.Select(k => k.ToString()));
        }

        public void Execute()
        {
            if (_commandInfo.HasNoInput())
                _commandInfo.Invoke();
            else
                GameObject
                    .FindObjectOfType<DevelopmentConsolePanel>()
                    .OpenCommandInputPromtFor(_commandInfo);
        }
    }
}
