
using Arman.Foundation.Core.PersistentDataManagement;
using System;

namespace Arman.Mocks.Foundation.Core.PersistentDataManagement
{
    public class PersistentDataSerializerMock : PersistentDataSerializer
    {
        public Action<WritablePersistentDataWrapper> onSerializeAction = delegate { };
        public Action<ReadablePersistentDataWrapper> onDeserializeAction = delegate { };

        int serializedCalls = 0;
        int deserializedCalls = 0;

        public bool IsSerializedCalledOnce()
        {
            return serializedCalls == 1;
        }

        public bool IsDeserializedCalledOnce()
        {
            return deserializedCalls == 1;
        }

        public bool IsSerialized()
        {
            return serializedCalls > 0;
        }

        public bool IsDeserialized()
        {
            return deserializedCalls > 0;
        }

        public void SerializeTo(WritablePersistentDataWrapper persistentDataWrapper)
        {
            serializedCalls++;
            onSerializeAction(persistentDataWrapper);
        }

        public void DeserializeFrom(ReadablePersistentDataWrapper persistentDataWrapper)
        {
            deserializedCalls++;
            onDeserializeAction(persistentDataWrapper);
        }
    }
}