using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Arman.UnityUtilities
{
    public class UnityAnimatorEventHandler : MonoBehaviour
    {
        [System.Serializable]
        public class AnimationEvent
        {
            [FormerlySerializedAs("name")]
            public string Name;

            [FormerlySerializedAs("action")]
            public UnityEvent Action;
        }

        [FormerlySerializedAs("animationEvents")]
        public List<AnimationEvent> AnimationEvents;

        void OnAnimationEvent(string eventName)
        {
            foreach (var animEvt in AnimationEvents)
                if (animEvt.Name.Equals(eventName))
                    animEvt.Action.Invoke();
        }
    }
}
