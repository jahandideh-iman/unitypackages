using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Arman.InGameMessageLogging.Samples
{
    public class LogInputController : MonoBehaviour
    {
        [FormerlySerializedAs("inGameMessageLogger")]
        public UnityInGameMessageLogger InGameMessageLogger;

        [FormerlySerializedAs("inputField")]
        public InputField InputField;

        public void Log()
        {
            InGameMessageLogger.Log(InputField.text);
        }
    }
}
