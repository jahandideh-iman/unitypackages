using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Arman.UnityUtilities.Tests
{
    public class DelayHandlerTests : UnityUtilitiesTestContext
    {
        private const float _shortDelay = 0.05f;
        private const float _longDelay = 60f;

        // Comfortably past the short delay, so a timer that should have fired has fired.
        private const float _waitPastShortDelay = 0.3f;

        private DelayHandler _handler = null!;
        private int _timeoutCount;

        [SetUp]
        public void SetUp()
        {
            _timeoutCount = 0;
            _handler = CreateComponent<DelayHandler>("DelayHandler");
            _handler.TimeoutEvent = new UnityEvent();
            _handler.TimeoutEvent.AddListener(() => _timeoutCount++);
        }

        [UnityTest]
        public IEnumerator StartTimer_RaisesTheTimeoutOnceTheDurationHasPassed()
        {
            _handler.Duration = _shortDelay;

            _handler.StartTimer();
            Assert.That(_timeoutCount, Is.EqualTo(0));
            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StartTimer_WithADuration_UsesItInsteadOfTheConfiguredOne()
        {
            _handler.Duration = _longDelay;

            _handler.StartTimer(_shortDelay);
            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StopTimer_PreventsTheTimeout()
        {
            _handler.Duration = _shortDelay;

            _handler.StartTimer();
            _handler.StopTimer();
            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator StartingTheTimerAgain_ReplacesTheRunningOne()
        {
            _handler.Duration = _longDelay;

            _handler.StartTimer();
            _handler.StartTimer(_shortDelay);
            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(1));

            _handler.StartTimer(_shortDelay);
            _handler.StartTimer(_shortDelay);
            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AutoStart_StartsTheTimerOnStart()
        {
            // Configured in the same frame it is added, so it is set before Start runs.
            var handler = CreateComponent<DelayHandler>("AutoStartingDelayHandler");
            handler.Duration = _shortDelay;
            handler.AutoStart = true;
            handler.TimeoutEvent = new UnityEvent();
            handler.TimeoutEvent.AddListener(() => _timeoutCount++);

            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WithoutAutoStart_TheTimerWaitsToBeStarted()
        {
            _handler.Duration = _shortDelay;

            yield return new WaitForSeconds(_waitPastShortDelay);

            Assert.That(_timeoutCount, Is.EqualTo(0));
        }
    }
}
