using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arman.UnityUtilities
{
    public class UnityEventDelegator : MonoBehaviour
    {
        [System.Serializable]
        public struct DelegationInfo
        {
            public string id;
            public UnityEvent delegates;
        }

        public List<DelegationInfo> delegations;

        public void DelegateAll()
        {
            foreach (var delegateInfo in delegations)
                delegateInfo.delegates.Invoke();
        }

        public void Delegate(string id)
        {
            foreach (var delegateInfo in delegations)
                if (delegateInfo.id.Equals(id))
                    delegateInfo.delegates.Invoke();
        }
    }
}
