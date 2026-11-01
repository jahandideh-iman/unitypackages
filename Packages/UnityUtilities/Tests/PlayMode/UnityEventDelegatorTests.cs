using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Events;

namespace Arman.UnityUtilities.Tests
{
    public class UnityEventDelegatorTests : UnityUtilitiesTestContext
    {
        private UnityEventDelegator _delegator = null!;
        private List<string> _invoked = null!;

        [SetUp]
        public void SetUp()
        {
            _invoked = new List<string>();
            _delegator = CreateComponent<UnityEventDelegator>("Delegator");
            _delegator.Delegations = new List<UnityEventDelegator.DelegationInfo>
            {
                Delegation("first"),
                Delegation("second"),
            };
        }

        [Test]
        public void Delegate_InvokesOnlyTheDelegationsWithTheGivenId()
        {
            _delegator.Delegate("second");

            Assert.That(_invoked, Is.EqualTo(new[] { "second" }));
        }

        [Test]
        public void Delegate_InvokesEveryDelegationThatSharesTheId()
        {
            _delegator.Delegations.Add(Delegation("first"));

            _delegator.Delegate("first");

            Assert.That(_invoked, Is.EqualTo(new[] { "first", "first" }));
        }

        [Test]
        public void Delegate_WithAnUnknownId_InvokesNothing()
        {
            _delegator.Delegate("unknown");

            Assert.That(_invoked, Is.Empty);
        }

        [Test]
        public void DelegateAll_InvokesEveryDelegationInOrder()
        {
            _delegator.DelegateAll();

            Assert.That(_invoked, Is.EqualTo(new[] { "first", "second" }));
        }

        private UnityEventDelegator.DelegationInfo Delegation(string id)
        {
            var delegates = new UnityEvent();
            delegates.AddListener(() => _invoked.Add(id));
            return new UnityEventDelegator.DelegationInfo { Id = id, Delegates = delegates };
        }
    }
}
