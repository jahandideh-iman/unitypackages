namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataSerializer
    {
        void SerializeTo(WritablePersistentDataWrapper persistentDataWrapper);
        void DeserializeFrom(ReadablePersistentDataWrapper persistentDataWrapper);
    }

}