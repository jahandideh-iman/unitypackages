using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Arman.Utilty.Unity
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
            foreach (var delgateInfo in delegations)
                delgateInfo.delegates.Invoke();
        }
        public void Delegate(string id)
        {
            foreach (var delgateInfo in delegations)
                if (delgateInfo.id.Equals(id))
                    delgateInfo.delegates.Invoke();
        }

    }
}