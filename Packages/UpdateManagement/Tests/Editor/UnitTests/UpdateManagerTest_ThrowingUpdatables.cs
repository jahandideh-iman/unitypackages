using System;
using System.Text.RegularExpressions;
using Arman.PackageBasics;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class UpdateManagerTest_ThrowingUpdatables
    {
        const string ThrownMessage = "updatable failed";

        static readonly Regex _expectedLog = new Regex(
            "InvalidOperationException: " + ThrownMessage
        );

        UpdateManager _manager = null!;
        IChannel _channel = null!;

        [SetUp]
        public void Setup()
        {
            _manager = new UpdateManager();
            _channel = new NamedChannel("Channel");
        }

        static Mock<IUpdatable> ThrowingUpdatable()
        {
            var throwing = new Mock<IUpdatable>();
            throwing
                .Setup(updatable => updatable.UpdateTime(It.IsAny<float>()))
                .Throws(new InvalidOperationException(ThrownMessage));
            return throwing;
        }

        [Test]
        public void AdvancingTimeShouldNotPropagateAnExceptionFromAnUpdatable()
        {
            _manager.RegisterUpdatable(ThrowingUpdatable().Object, _channel);

            LogAssert.Expect(LogType.Exception, _expectedLog);

            Assert.That(new TestDelegate(() => _manager.AdvanceTime(1f)), Throws.Nothing);
        }

        [Test]
        public void AnExceptionFromAnUpdatableShouldBeLogged()
        {
            _manager.RegisterUpdatable(ThrowingUpdatable().Object, _channel);

            LogAssert.Expect(LogType.Exception, _expectedLog);

            _manager.AdvanceTime(1f);
        }

        [Test]
        public void AThrowingUpdatableShouldNotStopTheRestOfTheChannelFromUpdating()
        {
            var wellBehaved = new Mock<IUpdatable>();

            // Registered on both sides of the offender: the channel is walked back to
            // front, so one of these would be skipped whichever order it aborted in.
            var first = new Mock<IUpdatable>();
            _manager.RegisterUpdatable(first.Object, _channel);
            _manager.RegisterUpdatable(ThrowingUpdatable().Object, _channel);
            _manager.RegisterUpdatable(wellBehaved.Object, _channel);

            LogAssert.Expect(LogType.Exception, _expectedLog);

            _manager.AdvanceTime(1f);

            first.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Once);
            wellBehaved.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Once);
        }

        [Test]
        public void AThrowingUpdatableShouldStayRegistered()
        {
            var throwing = ThrowingUpdatable();

            _manager.RegisterUpdatable(throwing.Object, _channel);

            LogAssert.Expect(LogType.Exception, _expectedLog);

            _manager.AdvanceTime(1f);

            Assert.That(_manager.Has(throwing.Object), Is.True);
        }

        [Test]
        public void AThrowingUpdatableShouldBeUpdatedAgainOnEveryLaterTick()
        {
            var throwing = ThrowingUpdatable();

            _manager.RegisterUpdatable(throwing.Object, _channel);

            LogAssert.Expect(LogType.Exception, _expectedLog);
            LogAssert.Expect(LogType.Exception, _expectedLog);
            LogAssert.Expect(LogType.Exception, _expectedLog);

            _manager.AdvanceTime(1f);
            _manager.AdvanceTime(1f);
            _manager.AdvanceTime(1f);

            throwing.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Exactly(3));
        }

        [Test]
        public void AnUpdatableThatDoesNotThrowShouldStayRegistered()
        {
            var wellBehaved = new Mock<IUpdatable>();

            _manager.RegisterUpdatable(wellBehaved.Object, _channel);
            _manager.AdvanceTime(1f);

            Assert.That(_manager.Has(wellBehaved.Object), Is.True);
            wellBehaved.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Once);
        }
    }
}
