using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Arman.UIManagement
{
    /// <summary>
    /// Animates a popup in or out. <see cref="UIManager"/> plays a popup's
    /// <see cref="PopupWindow.InTransition"/> when it opens and its
    /// <see cref="PopupWindow.OutTransition"/> before destroying it on close.
    /// </summary>
    public abstract class Transition : MonoBehaviour
    {
        /// <summary>
        /// Plays the transition and completes when it has finished. The token is cancelled when the
        /// transition is interrupted; the manager stops waiting at that point either way.
        /// </summary>
        public abstract UniTask Play(CancellationToken cancellationToken);
    }
}
