using System.Collections.Generic;
using Arman.PackageBasics;
using NUnit.Framework;
using UnityEngine;

namespace Arman.UpdateManagement.Tests
{
    /// <summary>
    /// The event is declared on <see cref="IUpdateManager"/> rather than only on
    /// <see cref="BasicUpdateManager"/> so that a consumer holding the interface -- which is
    /// all a Service Locator hands out -- can subscribe. These tests run against the
    /// interface deliberately: taking the concrete type would pass without that declaration.
    /// </summary>
    [TestFixture]
    public class UpdateManagerTest_ChannelStateChangedEvent
    {
        static IEnumerable<IUpdateManager> UpdateManagers()
        {
            yield return new BasicUpdateManager();
            yield return new GameObject(nameof(UnityUpdateManager)).AddComponent<UnityUpdateManager>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityUpdateManager manager in Object.FindObjectsByType<UnityUpdateManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void PausingARegisteredChannelShouldRaiseTheEvent(
            [ValueSource(nameof(UpdateManagers))] IUpdateManager manager)
        {
            IChannel channel = new NamedChannel("Channel");
            manager.RegisterChannel(channel);

            var raised = new List<(IChannel channel, bool isPaused)>();
            manager.ChannelStateChangedEvent += (c, p) => raised.Add((c, p));

            manager.Pause(channel);

            Assert.That(raised, Is.EqualTo(new[] { (channel, true) }));
        }

        [Test]
        public void ResumingARegisteredChannelShouldRaiseTheEvent(
            [ValueSource(nameof(UpdateManagers))] IUpdateManager manager)
        {
            IChannel channel = new NamedChannel("Channel");
            manager.RegisterChannel(channel);
            manager.Pause(channel);

            var raised = new List<(IChannel channel, bool isPaused)>();
            manager.ChannelStateChangedEvent += (c, p) => raised.Add((c, p));

            manager.Resume(channel);

            Assert.That(raised, Is.EqualTo(new[] { (channel, false) }));
        }

        [Test]
        public void PausingAnUnregisteredChannelShouldNotRaiseTheEvent(
            [ValueSource(nameof(UpdateManagers))] IUpdateManager manager)
        {
            var raised = new List<(IChannel channel, bool isPaused)>();
            manager.ChannelStateChangedEvent += (c, p) => raised.Add((c, p));

            manager.Pause(new NamedChannel("Unregistered"));

            Assert.That(raised, Is.Empty);
        }

        [Test]
        public void AnUnsubscribedHandlerShouldNotBeCalled(
            [ValueSource(nameof(UpdateManagers))] IUpdateManager manager)
        {
            IChannel channel = new NamedChannel("Channel");
            manager.RegisterChannel(channel);

            var raised = new List<(IChannel channel, bool isPaused)>();
            ChannelStateChanged handler = (c, p) => raised.Add((c, p));

            manager.ChannelStateChangedEvent += handler;
            manager.ChannelStateChangedEvent -= handler;
            manager.Pause(channel);

            Assert.That(raised, Is.Empty);
        }
    }
}
