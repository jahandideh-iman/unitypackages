using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Arman.InGameMessageLogging
{
    public class UnityInGameMessageLogger : MonoBehaviour, IInGameMessageLogger
    {
        [FormerlySerializedAs("loggerMessagePrefab")]
        public LogMessage LoggerMessagePrefab;

        [FormerlySerializedAs("messageContainer")]
        public GameObject MessageContainer;

        [FormerlySerializedAs("capacity")]
        public int Capacity;

        [FormerlySerializedAs("logLifeTime")]
        public float LogLifeTime;

        List<LogMessage> _logs = new List<LogMessage>();

        public void Log(string message)
        {
            if (_logs.Count >= Capacity)
                _logs[0].ClearSelf();

            var log = Instantiate(LoggerMessagePrefab, MessageContainer.transform, false)
                .Setup(message, LogLifeTime, RemoveLog);

            _logs.Add(log);
        }

        void RemoveLog(LogMessage log)
        {
            _logs.Remove(log);
        }
    }
}
