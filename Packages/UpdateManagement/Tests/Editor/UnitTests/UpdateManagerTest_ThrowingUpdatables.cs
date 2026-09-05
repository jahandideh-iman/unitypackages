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

        static readonly Regex ExpectedLog = new Regex("InvalidOperationException: " + ThrownMessage);

        UpdateManager manager = null!;
        IChannel channel = null!;

        [SetUp]
        public void Setup()
        {
            manager = new UpdateManager();
            channel = new NamedChannel("Channel");
        }

        static Mock<IUpdatable> ThrowingUpdatable()
        {
            var throwing = new Mock<IUpdatable>();
            throwing.Setup(updatable => updatable.UpdateTime(It.IsAny<float>()))
                .Throws(new InvalidOperationException(ThrownMessage));
            return throwing;
        }

        [Test]
        public void AdvancingTimeShouldNotPropagateAnExceptionFromAnUpdatable()
        {
            manager.RegisterUpdatable(ThrowingUpdatable().Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            Assert.That(new TestDelegate(() => manager.AdvanceTime(1f)), Throws.Nothing);
        }

        [Test]
        public void AnExceptionFromAnUpdatableShouldBeLogged()
        {
            manager.RegisterUpdatable(ThrowingUpdatable().Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);
        }

        [Test]
        public void AThrowingUpdatableShouldNotStopTheRestOfTheChannelFromUpdating()
        {
            var wellBehaved = new Mock<IUpdatable>();

            // Registered on both sides of the offender: the channel is walked back to
            // front, so one of these would be skipped whichever order it aborted in.
            var first = new Mock<IUpdatable>();
            manager.RegisterUpdatable(first.Object, channel);
            manager.RegisterUpdatable(ThrowingUpdatable().Object, channel);
            manager.RegisterUpdatable(wellBehaved.Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);

            first.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Once);
            wellBehaved.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Once);
        }

        [Test]
        public void AThrowingUpdatableShouldStayRegistered()
        {
            var throwing = ThrowingUpdatable();

            manager.RegisterUpdatable(throwing.Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);

            Assert.That(manager.Has(throwing.Object), Is.True);
        }

        [Test]
        public void AThrowingUpdatableShouldBeUpdatedAgainOnEveryLaterTick()
        {
            var throwing = ThrowingUpdatable();

            manager.RegisterUpdatable(throwing.Object, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);
            LogAssert.Expect(LogType.Exception, ExpectedLog);
            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);

            throwing.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Exactly(3));
        }

        [Test]
        public void AnUpdatableThatDoesNotThrowShouldStayRegistered()
        {
            var wellBehaved = new Mock<IUpdatable>();

            manager.RegisterUpdatable(wellBehaved.Object, channel);
            manager.AdvanceTime(1f);

            Assert.That(manager.Has(wellBehaved.Object), Is.True);
            wellBehaved.Verify(updatable => updatable.UpdateTime(It.IsAny<float>()), Times.Once);
        }
    }
}
