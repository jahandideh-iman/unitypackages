namespace Arman.PersistentDataManagement
{
    public interface IPersistentDataSerializer
    {
        string Key();

        void SerializeTo(IWritablePersistentDataEncoder persistentDataEncoder);
        void DeserializeFrom(IReadablePersistentDataEncoder persistentDataEncoder);
    }
}
