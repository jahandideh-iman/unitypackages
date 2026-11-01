using System;
using Arman.UnityUtilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Arman.InGameMessageLogging
{
    public class LogMessage : MonoBehaviour
    {
        [FormerlySerializedAs("setTextAction")]
        public StringUnityEvent SetTextAction;

        [FormerlySerializedAs("startTimeAction")]
        public FloatUnityEvent StartTimeAction;

        [FormerlySerializedAs("fadeInAction")]
        public UnityEvent FadeInAction;

        [FormerlySerializedAs("fadeOutAction")]
        public UnityEvent FadeOutAction;

        Action<LogMessage> _clearCallback;

        string _message;

        public LogMessage Setup(string message, float lifeTime, Action<LogMessage> clearCallback)
        {
            this._message = message;

            this._clearCallback = clearCallback;
            SetTextAction.Invoke(message);
            FadeInAction.Invoke();

            StartTimeAction.Invoke(lifeTime);

            return this;
        }

        public string Message()
        {
            return _message;
        }

        public void FadeOut()
        {
            FadeOutAction.Invoke();
        }

        public void ClearSelf()
        {
            _clearCallback(this);
            Destroy(this.gameObject);
        }
    }
}
