using System;
using UnityEngine;


namespace Arman.Development.DevelopmentConsole.Base
{
        public class ShortCutInfo
        {
            public KeyCode[] keyCodes;
            public Action action;

            public ShortCutInfo(KeyCode[] keyCodes, Action action)
            {
                this.keyCodes = keyCodes;
                this.action = action;
            }
        }
}