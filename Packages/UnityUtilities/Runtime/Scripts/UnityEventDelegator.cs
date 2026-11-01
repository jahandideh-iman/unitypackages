using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Arman.UnityUtilities
{
    public class UnityEventDelegator : MonoBehaviour
    {
        [System.Serializable]
        public struct DelegationInfo
        {
            [FormerlySerializedAs("id")]
            public string Id;

            [FormerlySerializedAs("delegates")]
            public UnityEvent Delegates;
        }

        [FormerlySerializedAs("delegations")]
        public List<DelegationInfo> Delegations;

        public void DelegateAll()
        {
            foreach (var delegateInfo in Delegations)
                delegateInfo.Delegates.Invoke();
        }

        public void Delegate(string id)
        {
            foreach (var delegateInfo in Delegations)
                if (delegateInfo.Id.Equals(id))
                    delegateInfo.Delegates.Invoke();
        }
    }
}
