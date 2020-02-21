using UnityEngine;


namespace Arman.Utilty.Unity
{
    [RequireComponent(typeof(Animation))]
    public class UnityAnimationPlayer : MonoBehaviour
    {
        Animation animationComp;

        private void Awake()
        {
            animationComp = GetComponent<Animation>();
        }

        public void Play(string animationName)
        {
            animationComp.Stop();
            animationComp.Play(animationName);
        }

        public void Play()
        {
            animationComp.Stop();
            animationComp.Play();
        }

        public void Play(AnimationClip animationClip)
        {
            Play(animationClip, animationClip.name);
        }

        public void Play(AnimationClip animationClip, string animationName)
        {
            animationComp.Stop();
            animationComp.AddClip(animationClip, animationName);
            animationComp.Play(animationName);
        }

        public void RemoveClip(string animationName)
        {
            animationComp.RemoveClip(animationName);
        }

        public void Stop()
        {
            animationComp.Stop();
        }
    }
}