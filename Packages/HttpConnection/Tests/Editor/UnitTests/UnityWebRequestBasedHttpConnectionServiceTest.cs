using NUnit.Framework;
using UnityEngine;

namespace Arman.HttpConnection.Tests
{
    public class UnityWebRequestBasedHttpConnectionServiceTest
    {
        GameObject _gameObject = null!;
        UnityWebRequestBasedHttpConnectionService _service = null!;

        [SetUp]
        public void Setup()
        {
            _gameObject = new GameObject("HttpConnectionService");
            _service = _gameObject.AddComponent<UnityWebRequestBasedHttpConnectionService>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void TheTimeoutMessageShouldBeRecognisedAsATimeout()
        {
            Assert.That(_service.IsTimeOut("Timeout"), Is.True);
        }

        [Test]
        public void OtherMessagesShouldNotBeRecognisedAsATimeout()
        {
            Assert.That(_service.IsTimeOut("Not Found"), Is.False);
            Assert.That(_service.IsTimeOut(""), Is.False);
            Assert.That(_service.IsTimeOut("timeout"), Is.False);
        }
    }
}
