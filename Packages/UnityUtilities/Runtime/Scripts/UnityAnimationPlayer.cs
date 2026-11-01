using UnityEngine;

namespace Arman.UnityUtilities
{
    [RequireComponent(typeof(Animation))]
    public class UnityAnimationPlayer : MonoBehaviour
    {
        Animation _animationComp;

        private void Awake()
        {
            _animationComp = GetComponent<Animation>();
        }

        public void Play(string animationName)
        {
            _animationComp.Stop();
            _animationComp.Play(animationName);
        }

        public void Play()
        {
            _animationComp.Stop();
            _animationComp.Play();
        }

        public void Play(AnimationClip animationClip)
        {
            Play(animationClip, animationClip.name);
        }

        public void Play(AnimationClip animationClip, string animationName)
        {
            _animationComp.Stop();
            _animationComp.AddClip(animationClip, animationName);
            _animationComp.Play(animationName);
        }

        public void RemoveClip(string animationName)
        {
            _animationComp.RemoveClip(animationName);
        }

        public void Stop()
        {
            _animationComp.Stop();
        }
    }
}
