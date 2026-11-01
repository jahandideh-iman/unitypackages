using System.Collections;
using System.Collections.Generic;
using Arman.UnityUtilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Arman.InGameMessageLogging.Tests
{
    public class LogMessageTests
    {
        readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void Teardown()
        {
            foreach (var gameObject in _created)
                if (gameObject != null)
                    Object.Destroy(gameObject);

            _created.Clear();
        }

        [Test]
        public void Setup_ShowsTheMessageFadesInAndStartsTheTimer()
        {
            var log = CreateLogMessage();
            string? shownText = null;
            float? startedTime = null;
            var fadedIn = false;
            log.SetTextAction.AddListener(text => shownText = text);
            log.StartTimeAction.AddListener(time => startedTime = time);
            log.FadeInAction.AddListener(() => fadedIn = true);

            log.Setup("hello", 3f, delegate { });

            Assert.That(shownText, Is.EqualTo("hello"));
            Assert.That(startedTime, Is.EqualTo(3f));
            Assert.That(fadedIn, Is.True);
        }

        [Test]
        public void Setup_ReturnsTheLogAndRemembersTheMessage()
        {
            var log = CreateLogMessage();

            var result = log.Setup("hello", 3f, delegate { });

            Assert.That(result, Is.SameAs(log));
            Assert.That(log.Message(), Is.EqualTo("hello"));
        }

        [Test]
        public void FadeOut_PlaysTheFadeOutAction()
        {
            var log = CreateLogMessage();
            var fadedOut = false;
            log.FadeOutAction.AddListener(() => fadedOut = true);
            log.Setup("hello", 3f, delegate { });

            log.FadeOut();

            Assert.That(fadedOut, Is.True);
        }

        [UnityTest]
        public IEnumerator ClearSelf_HandsTheLogToTheClearCallbackAndDestroysIt()
        {
            var log = CreateLogMessage();
            LogMessage? cleared = null;
            log.Setup("hello", 3f, l => cleared = l);

            log.ClearSelf();
            yield return null;

            Assert.That(cleared, Is.SameAs(log));
            Assert.That(log == null, Is.True);
        }

        private LogMessage CreateLogMessage()
        {
            var gameObject = new GameObject("LogMessage");
            _created.Add(gameObject);

            var log = gameObject.AddComponent<LogMessage>();
            log.SetTextAction = new StringUnityEvent();
            log.StartTimeAction = new FloatUnityEvent();
            log.FadeInAction = new UnityEvent();
            log.FadeOutAction = new UnityEvent();
            return log;
        }
    }
}
