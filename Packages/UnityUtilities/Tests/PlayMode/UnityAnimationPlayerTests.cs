using NUnit.Framework;
using UnityEngine;

namespace Arman.UnityUtilities.Tests
{
    public class UnityAnimationPlayerTests : UnityUtilitiesTestContext
    {
        private UnityAnimationPlayer _player = null!;
        private Animation _animation = null!;

        [SetUp]
        public void SetUp()
        {
            _player = CreateComponent<UnityAnimationPlayer>("AnimationPlayer");
            _animation = _player.GetComponent<Animation>();
        }

        [Test]
        public void AddingThePlayer_AddsTheAnimationItRequires()
        {
            Assert.That(_animation != null, Is.True);
        }

        [Test]
        public void PlayingAClip_AddsItUnderItsOwnNameAndPlaysIt()
        {
            var clip = CreateClip("Clip");

            _player.Play(clip);

            Assert.That(_animation.GetClip("Clip") != null, Is.True);
            Assert.That(_animation.IsPlaying("Clip"), Is.True);
        }

        [Test]
        public void PlayingAClipUnderAName_AddsItUnderThatNameAndPlaysIt()
        {
            var clip = CreateClip("Clip");

            _player.Play(clip, "Renamed");

            Assert.That(_animation.GetClip("Renamed") != null, Is.True);
            Assert.That(_animation.IsPlaying("Renamed"), Is.True);
        }

        [Test]
        public void PlayingAnotherClip_StopsTheOneThatWasPlaying()
        {
            _player.Play(CreateClip("First"));

            _player.Play(CreateClip("Second"));

            Assert.That(_animation.IsPlaying("First"), Is.False);
            Assert.That(_animation.IsPlaying("Second"), Is.True);
        }

        [Test]
        public void PlayingByName_PlaysAClipThatWasAddedBefore()
        {
            _player.Play(CreateClip("Clip"));
            _player.Stop();

            _player.Play("Clip");

            Assert.That(_animation.IsPlaying("Clip"), Is.True);
        }

        [Test]
        public void Stop_StopsThePlayingClip()
        {
            _player.Play(CreateClip("Clip"));

            _player.Stop();

            Assert.That(_animation.isPlaying, Is.False);
        }

        [Test]
        public void RemoveClip_RemovesTheClipFromTheAnimation()
        {
            _player.Play(CreateClip("Clip"));
            _player.Stop();

            _player.RemoveClip("Clip");

            Assert.That(_animation.GetClip("Clip") == null, Is.True);
        }

        // Legacy, so the Animation component can play it, and a second long, so it is
        // still playing when the test looks.
        private AnimationClip CreateClip(string name)
        {
            var clip = Track(new AnimationClip { name = name, legacy = true });
            clip.SetCurve(
                "",
                typeof(Transform),
                "localPosition.x",
                AnimationCurve.Linear(0f, 0f, 1f, 1f)
            );
            return clip;
        }
    }
}
