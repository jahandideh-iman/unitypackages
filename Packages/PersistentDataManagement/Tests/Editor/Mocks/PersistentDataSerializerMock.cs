
using System;

namespace Arman.PersistentDataManagement.Tests
{
    public class PersistentDataSerializerMock : IPersistentDataSerializer
    {
        public Action<IWritablePersistentDataWrapper> onSerializeAction = delegate { };
        public Action<IReadablePersistentDataWrapper> onDeserializeAction = delegate { };

        int serializedCalls = 0;
        int deserializedCalls = 0;
        string key;

        public PersistentDataSerializerMock(string key)
        {
            this.key = key;
        }

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

        public string Key()
        {
            return key;
        }

        public void SerializeTo(IWritablePersistentDataWrapper persistentDataWrapper)
        {
            serializedCalls++;
            onSerializeAction(persistentDataWrapper);
        }

        public void DeserializeFrom(IReadablePersistentDataWrapper persistentDataWrapper)
        {
            deserializedCalls++;
            onDeserializeAction(persistentDataWrapper);
        }

    }
}