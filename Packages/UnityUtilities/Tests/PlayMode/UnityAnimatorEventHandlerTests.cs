using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Arman.UnityUtilities.Tests
{
    public class UnityAnimatorEventHandlerTests : UnityUtilitiesTestContext
    {
        private UnityAnimatorEventHandler _handler = null!;
        private List<string> _invoked = null!;

        [SetUp]
        public void SetUp()
        {
            _invoked = new List<string>();
            _handler = CreateComponent<UnityAnimatorEventHandler>("Handler");
            _handler.AnimationEvents = new List<UnityAnimatorEventHandler.AnimationEvent>
            {
                AnimationEvent("Hit"),
                AnimationEvent("Land"),
            };
        }

        // An animation event reaches the handler by name, the way SendMessage delivers it.
        [Test]
        public void AnAnimationEvent_InvokesTheActionsWithItsName()
        {
            _handler.gameObject.SendMessage("OnAnimationEvent", "Land");

            Assert.That(_invoked, Is.EqualTo(new[] { "Land" }));
        }

        [Test]
        public void AnUnknownAnimationEvent_InvokesNothing()
        {
            _handler.gameObject.SendMessage("OnAnimationEvent", "Unknown");

            Assert.That(_invoked, Is.Empty);
        }

        private UnityAnimatorEventHandler.AnimationEvent AnimationEvent(string name)
        {
            var action = new UnityEvent();
            action.AddListener(() => _invoked.Add(name));
            return new UnityAnimatorEventHandler.AnimationEvent { Name = name, Action = action };
        }
    }
}
