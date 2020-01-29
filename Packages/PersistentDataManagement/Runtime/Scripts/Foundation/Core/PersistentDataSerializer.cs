namespace Arman.Foundation.Core.PersistentDataManagement
{
    public interface PersistentDataSerializer
    {
        string Key();

        void SerializeTo(WritablePersistentDataWrapper persistentDataWrapper);
        void DeserializeFrom(ReadablePersistentDataWrapper persistentDataWrapper);
    }

}