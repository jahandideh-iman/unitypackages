using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Arman.SceneManagement.Tests
{
    public class SceneInitializerTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void Teardown()
        {
            foreach (var gameObject in _created)
                if (gameObject != null)
                    Object.Destroy(gameObject);

            _created.Clear();
        }

        [Test]
        public void Awake_InitializesTheScene()
        {
            var initializer = Create("Initializer").AddComponent<TestSceneInitializer>();

            Assert.That(initializer.InitCount, Is.EqualTo(1));
        }

        [Test]
        public void AnInactiveInitializer_InitializesOnlyOnceActivated()
        {
            var gameObject = Create("Initializer");
            gameObject.SetActive(false);
            var initializer = gameObject.AddComponent<TestSceneInitializer>();

            Assert.That(initializer.InitCount, Is.EqualTo(0));

            gameObject.SetActive(true);

            Assert.That(initializer.InitCount, Is.EqualTo(1));
        }

        [Test]
        public void ReactivatingTheInitializer_DoesNotInitializeTheSceneAgain()
        {
            var gameObject = Create("Initializer");
            var initializer = gameObject.AddComponent<TestSceneInitializer>();

            gameObject.SetActive(false);
            gameObject.SetActive(true);

            Assert.That(initializer.InitCount, Is.EqualTo(1));
        }

        [Test]
        public void TheInitializer_RunsBeforeScriptsWithTheDefaultExecutionOrder()
        {
            var attributes = typeof(SceneInitializer).GetCustomAttributes(
                typeof(DefaultExecutionOrder),
                false
            );

            Assert.That(attributes, Has.Length.EqualTo(1));
            Assert.That(((DefaultExecutionOrder)attributes[0]).order, Is.LessThan(0));
        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _created.Add(gameObject);
            return gameObject;
        }

        private class TestSceneInitializer : SceneInitializer
        {
            public int InitCount { get; private set; }

            protected override void Init()
            {
                InitCount++;
            }
        }
    }
}
