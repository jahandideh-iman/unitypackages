using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.Development.DevelopmentConsole.Base
{

    public class DevelopmentGroup : MonoBehaviour
    {
        public DevelopmentCommand commandPrefab;

        public Text gourpNameText;

        public void Init(string groupName)
        {
            this.gourpNameText.text = groupName;
        }

        public void AddCommand(CommandInfo commandInfo)
        {
            var commandObject = Instantiate(commandPrefab, this.transform, false);
            commandObject.Init(commandInfo);
        }
    }
}