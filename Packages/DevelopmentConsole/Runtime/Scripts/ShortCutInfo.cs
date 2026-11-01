using System;
using UnityEngine;

namespace Arman.DevelopmentConsole
{
    public class ShortCutInfo
    {
        public KeyCode[] KeyCodes;
        public Action Action;

        public ShortCutInfo(KeyCode[] keyCodes, Action action)
        {
            this.KeyCodes = keyCodes;
            this.Action = action;
        }
    }
}
