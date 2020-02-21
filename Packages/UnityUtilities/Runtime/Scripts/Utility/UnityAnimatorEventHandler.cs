using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arman.Utilty.Unity
{
    public class UnityAnimatorEventHandler : MonoBehaviour
    {
        [System.Serializable]
        public class AnimationEvent
        {
            public string name;
            public UnityEvent action;
        }

        public List<AnimationEvent> animationEvents;

        void OnAnimationEvent(string eventName)
        {
            foreach (var animEvt in animationEvents)
                if (animEvt.name.Equals(eventName))
                    animEvt.action.Invoke();
        }

    }
}