using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Arman.UnityUtilities
{
    public class DelayHandler : MonoBehaviour
    {
        [FormerlySerializedAs("id")]
        public string Id;

        [FormerlySerializedAs("duration")]
        public float Duration;

        [FormerlySerializedAs("autoStart")]
        public bool AutoStart = false;

        [FormerlySerializedAs("timeoutEvent")]
        public UnityEvent TimeoutEvent;

        void Start()
        {
            if (AutoStart)
                StartTimer();
        }

        public void StartTimer()
        {
            StopAllCoroutines();
            StartCoroutine(WaitFor(Duration));
        }

        public void StartTimer(float newDuration)
        {
            StopAllCoroutines();
            StartCoroutine(WaitFor(newDuration));
        }

        public void StopTimer()
        {
            StopAllCoroutines();
        }

        private IEnumerator WaitFor(float duration)
        {
            yield return new WaitForSeconds(duration);
            TimeoutEvent.Invoke();
        }
    }
}
