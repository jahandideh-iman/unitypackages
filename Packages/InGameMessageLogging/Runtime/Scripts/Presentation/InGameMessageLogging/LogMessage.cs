

using Arman.Utilty.Unity;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Arman.Presentation.InGameMessageLogging
{
    public class LogMessage : MonoBehaviour
    {
        public StringUnityEvent setTextAction;

        public FloatUnityEvent startTimeAction;
        public UnityEvent fadeInAction;
        public UnityEvent fadeOutAction;

        Action<LogMessage> clearCallback;

        string message;

        public LogMessage Setup(string message, float lifeTime, Action<LogMessage> clearCallback)
        {
            this.message = message;

            this.clearCallback = clearCallback;
            setTextAction.Invoke(message);
            fadeInAction.Invoke();

            startTimeAction.Invoke(lifeTime);

            return this;
        }

        public string Message()
        {
            return message;
        }

        public void FadeOut()
        {
            fadeOutAction.Invoke();
        }

        public void ClearSelf()
        {
            clearCallback(this);
            Destroy(this.gameObject);
        }
    }
}