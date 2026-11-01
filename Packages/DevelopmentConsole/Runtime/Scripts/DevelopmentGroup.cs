using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.DevelopmentConsole
{
    public class DevelopmentGroup : MonoBehaviour
    {
        [FormerlySerializedAs("commandPrefab")]
        public DevelopmentCommand CommandPrefab;

        [FormerlySerializedAs("gourpNameText")]
        public Text GourpNameText;

        public void Init(string groupName)
        {
            this.GourpNameText.text = groupName;
        }

        public void AddCommand(CommandInfo commandInfo)
        {
            var commandObject = Instantiate(CommandPrefab, this.transform, false);
            commandObject.Init(commandInfo);
        }
    }
}
