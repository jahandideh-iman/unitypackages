using System;
using Arman.PackageBasics;
using NUnit.Framework;

namespace Arman.UpdateManagement.Tests
{
    [TestFixture]
    public class BasicUpdateManagerTest_ThrowingUpdatables
    {
        BasicUpdateManager manager = null!;
        IChannel channel = null!;

        [SetUp]
        public void Setup()
        {
            manager = new BasicUpdateManager();
            channel = new NamedChannel("Channel");
        }

        [Test]
        public void AdvancingTimeShouldNotPropagateAnExceptionFromAnUpdatable()
        {
            var throwing = new UpdatableMock();
            throwing.onUpdateAction = _ => throw new InvalidOperationException();

            manager.RegisterUpdatable(throwing, channel);

            Assert.That(new TestDelegate(() => manager.AdvanceTime(1f)), Throws.Nothing);
        }

        [Test]
        public void AThrowingUpdatableShouldNotStopTheRestOfTheChannelFromUpdating()
        {
            var throwing = new UpdatableMock();
            throwing.onUpdateAction = _ => throw new InvalidOperationException();
            var wellBehaved = new UpdatableMock();

            // Registered on both sides of the offender: the channel is walked back to
            // front, so one of these would be skipped whichever order it aborted in.
            var first = new UpdatableMock();
            manager.RegisterUpdatable(first, channel);
            manager.RegisterUpdatable(throwing, channel);
            manager.RegisterUpdatable(wellBehaved, channel);

            manager.AdvanceTime(1f);

            Assert.That(first.IsUpdated(), Is.True);
            Assert.That(wellBehaved.IsUpdated(), Is.True);
        }

        [Test]
        public void AThrowingUpdatableShouldBeUnregistered()
        {
            var throwing = new UpdatableMock();
            throwing.onUpdateAction = _ => throw new InvalidOperationException();

            manager.RegisterUpdatable(throwing, channel);
            manager.AdvanceTime(1f);

            Assert.That(manager.Has(throwing), Is.False);
        }

        [Test]
        public void AThrowingUpdatableShouldNotBeUpdatedAgainOnALaterTick()
        {
            var throwing = new UpdatableMock();
            throwing.onUpdateAction = _ => throw new InvalidOperationException();

            manager.RegisterUpdatable(throwing, channel);
            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);
            manager.AdvanceTime(1f);

            Assert.That(throwing.UpdateCallCount(), Is.EqualTo(1));
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
