

using Arman.Foundation.InGameMessageLogging;
using System.Collections.Generic;
using UnityEngine;

namespace Arman.Presentation.InGameMessageLogging
{
    public class UnityInGameMessageLogger : MonoBehaviour, InGameMessageLogger
    {
        public LogMessage loggerMessagePrefab;

        public GameObject messageContainer;

        public int capacity;
        public float logLifeTime;


        List<LogMessage> logs = new List<LogMessage>();


        public void Log(string message)
        {
            if (logs.Count >= capacity)
                logs[0].ClearSelf();
            
            var log = Instantiate(loggerMessagePrefab, messageContainer.transform , false)
                .Setup(message, logLifeTime, RemoveLog);

            logs.Add(log);
        }

        void RemoveLog(LogMessage log)
        {
            logs.Remove(log);
        }
    }
}