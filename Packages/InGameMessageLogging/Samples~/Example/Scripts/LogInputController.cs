using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arman.InGameMessageLogging.Samples
{
    public class LogInputController : MonoBehaviour
    {
        public UnityInGameMessageLogger inGameMessageLogger;
        public InputField inputField;

        public void Log()
        {
            inGameMessageLogger.Log(inputField.text);
        }

    }
}