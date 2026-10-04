using System;
using UnityEngine;

namespace Arman.UIManagement
{
    // Put on a popup's root GameObject to animate it in and out. UIManager blocks
    // input from the moment a transition starts until it calls onComplete, so a
    // transition must always end by calling onComplete. A PlayOut that starts while
    // PlayIn is still running replaces it; the PlayIn callback is then ignored.
    public abstract class PopupTransition : MonoBehaviour
    {
        public abstract void PlayIn(Action onComplete);

        public abstract void PlayOut(Action onComplete);
    }
}
