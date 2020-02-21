using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Arman.Utilty.Unity
{
    public class DelayHandler : MonoBehaviour
    {
        public string id;
		public float duration;
        public bool autoStart = false;

        public UnityEvent timeoutEvent;


        void Start()
        {
            if (autoStart)
                StartTimer();
        }

		public void StartTimer()
		{
			StopAllCoroutines();
			StartCoroutine(WaitFor(duration));
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
			timeoutEvent.Invoke ();
		}

	}
}