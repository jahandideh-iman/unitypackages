using System;
using System.Text.RegularExpressions;
using Arman.PackageBasics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class BasicUpdateManagerTest_ThrowingUpdatables
    {
        const string ThrownMessage = "updatable failed";

        static readonly Regex ExpectedLog = new Regex("InvalidOperationException: " + ThrownMessage);

        BasicUpdateManager manager = null!;
        IChannel channel = null!;

        [SetUp]
        public void Setup()
        {
            manager = new BasicUpdateManager();
            channel = new NamedChannel("Channel");
        }

        static UpdatableMock ThrowingUpdatable()
        {
            var throwing = new UpdatableMock();
            throwing.onUpdateAction = _ => throw new InvalidOperationException(ThrownMessage);
            return throwing;
        }

        [Test]
        public void AdvancingTimeShouldNotPropagateAnExceptionFromAnUpdatable()
        {
            manager.RegisterUpdatable(ThrowingUpdatable(), channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            Assert.That(new TestDelegate(() => manager.AdvanceTime(1f)), Throws.Nothing);
        }

        [Test]
        public void AnExceptionFromAnUpdatableShouldBeLogged()
        {
            manager.RegisterUpdatable(ThrowingUpdatable(), channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);
        }

        [Test]
        public void AThrowingUpdatableShouldNotStopTheRestOfTheChannelFromUpdating()
        {
            var wellBehaved = new UpdatableMock();

            // Registered on both sides of the offender: the channel is walked back to
            // front, so one of these would be skipped whichever order it aborted in.
            var first = new UpdatableMock();
            manager.RegisterUpdatable(first, channel);
            manager.RegisterUpdatable(ThrowingUpdatable(), channel);
            manager.RegisterUpdatable(wellBehaved, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);

            Assert.That(first.IsUpdated(), Is.True);
            Assert.That(wellBehaved.IsUpdated(), Is.True);
        }

        [Test]
        public void AThrowingUpdatableShouldStayRegistered()
        {
            var throwing = ThrowingUpdatable();

            manager.RegisterUpdatable(throwing, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);

            Assert.That(manager.Has(throwing), Is.True);
        }

        [Test]
        public void AThrowingUpdatableShouldBeUpdatedAgainOnEveryLaterTick()
        {
            var throwing = ThrowingUpdatable();

            manager.RegisterUpdatable(throwing, channel);

            LogAssert.Expect(LogType.Exception, ExpectedLog);
            LogAssert.Expect(LogType.Exception, ExpectedLog);
            LogAssert.Expect(LogType.Exception, ExpectedLog);

            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);

            Assert.That(throwing.UpdateCallCount(), Is.EqualTo(3));
        }

        [Test]
        public void AnUpdatableThatDoesNotThrowShouldStayRegistered()
        {
            var wellBehaved = new UpdatableMock();

            manager.RegisterUpdatable(wellBehaved, channel);
            manager.AdvanceTime(1f);

            Assert.That(manager.Has(wellBehaved), Is.True);
            Assert.That(wellBehaved.UpdateCallCount(), Is.EqualTo(1));
        }
    }
}
