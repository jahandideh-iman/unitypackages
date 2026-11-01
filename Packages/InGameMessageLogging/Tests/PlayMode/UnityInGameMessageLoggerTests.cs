using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arman.UnityUtilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Arman.InGameMessageLogging.Tests
{
    public class UnityInGameMessageLoggerTests
    {
        private const int _capacity = 2;

        private readonly List<GameObject> _created = new List<GameObject>();

        private UnityInGameMessageLogger _logger = null!;
        private Transform _container = null!;

        [SetUp]
        public void SetUp()
        {
            var prefab = Create("LogMessagePrefab").AddComponent<LogMessage>();
            prefab.SetTextAction = new StringUnityEvent();
            prefab.StartTimeAction = new FloatUnityEvent();
            prefab.FadeInAction = new UnityEvent();
            prefab.FadeOutAction = new UnityEvent();

            _container = Create("MessageContainer").transform;

            _logger = Create("Logger").AddComponent<UnityInGameMessageLogger>();
            _logger.LoggerMessagePrefab = prefab;
            _logger.MessageContainer = _container.gameObject;
            _logger.Capacity = _capacity;
            _logger.LogLifeTime = 3f;
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var gameObject in _created)
                if (gameObject != null)
                    Object.Destroy(gameObject);

            _created.Clear();
        }

        [Test]
        public void Log_ShowsTheMessageUnderTheContainer()
        {
            _logger.Log("hello");

            Assert.That(ShownMessages(), Is.EqualTo(new[] { "hello" }));
        }

        [Test]
        public void Log_BelowCapacity_KeepsEveryMessageInOrder()
        {
            _logger.Log("first");
            _logger.Log("second");

            Assert.That(ShownMessages(), Is.EqualTo(new[] { "first", "second" }));
        }

        [UnityTest]
        public IEnumerator Log_AtCapacity_ClearsTheOldestMessage()
        {
            _logger.Log("first");
            _logger.Log("second");
            _logger.Log("third");
            yield return null;

            Assert.That(ShownMessages(), Is.EqualTo(new[] { "second", "third" }));
        }

        [UnityTest]
        public IEnumerator Log_WellPastCapacity_KeepsOnlyTheNewestMessages()
        {
            foreach (var message in new[] { "1", "2", "3", "4", "5" })
                _logger.Log(message);
            yield return null;

            Assert.That(ShownMessages(), Is.EqualTo(new[] { "4", "5" }));
        }

        [UnityTest]
        public IEnumerator AMessageThatClearsItself_FreesItsPlaceForTheNextOne()
        {
            _logger.Log("first");
            _logger.Log("second");
            FindLog("first").ClearSelf();
            yield return null;

            _logger.Log("third");
            yield return null;

            Assert.That(ShownMessages(), Is.EqualTo(new[] { "second", "third" }));
        }

        private string[] ShownMessages()
        {
            return Logs().Select(log => log.Message()).ToArray();
        }

        private LogMessage FindLog(string message)
        {
            return Logs().First(log => log.Message() == message);
        }

        private IEnumerable<LogMessage> Logs()
        {
            return Enumerable
                .Range(0, _container.childCount)
                .Select(i => _container.GetChild(i).GetComponent<LogMessage>());
        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _created.Add(gameObject);
            return gameObject;
        }
    }
}
