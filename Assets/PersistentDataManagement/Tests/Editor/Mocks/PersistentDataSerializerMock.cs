
using Arman.Foundation.Core.PersistentDataManagement;
using System;

namespace Arman.Mocks.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataSerializerMock : PersistentDataSerializer
    {
        public Action<PersistentDataWrapper> onSerializeAction = delegate { };

        int serializedCalls = 0;

        public bool IsSerializedCalledOnce()
        {
            return serializedCalls == 1;
        }

        public bool IsSerialized()
        {
            return serializedCalls > 0;
        }

        public void Serialize(PersistentDataWrapper persistentDataWrapper)
        {
            serializedCalls++;
            onSerializeAction(persistentDataWrapper);
        }
    }
}